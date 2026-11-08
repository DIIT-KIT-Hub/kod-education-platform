using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Admin.Roles;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Imports users from an XLSX/CSV file (one row per position) into the organizations the current user administers.
/// Nothing is saved when any row is invalid; a dry run only validates.
/// </summary>
public static class ImportUsers
{
    /// <summary>
    /// Accepted values of the «Статус» column meaning a blocked account.
    /// </summary>
    private static readonly string[] BlockedStatuses = ["заблокований", "заблоковано", "blocked", "inactive", "0", "ні", "no"];

    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="File">The uploaded file.</param>
    /// <param name="DryRun">Whether the file is only validated.</param>
    [RequirePermission(Permissions.Users.Import)]
    public sealed record Command(FileUpload File, bool DryRun) : ICommand<Result<UserImportReport>>;

    /// <summary>
    /// Validates every row, then creates and updates the accounts in one transaction and sends the invitations.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="permissions">Resolves effective permissions.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="spreadsheet">Reads the file.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        IIdentityService identity,
        IPermissionResolver permissions,
        IAccessScope accessScope,
        IUserSpreadsheet spreadsheet,
        UserDirectory directory,
        IAuditLogger audit) : IRequestHandler<Command, Result<UserImportReport>>
    {
        /// <inheritdoc />
        public async Task<Result<UserImportReport>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var file = request.File;
            if (file.Length > FileRules.MaxImportSize)
            {
                return Error.Validation(Texts.T("Файл імпорту не може бути більшим за 5 МБ."));
            }

            var format = UserDirectory.DetectFormat(file.FileName);
            if (format is null)
            {
                return Error.Validation(Texts.T("Підтримуються файли .xlsx та .csv."));
            }

            IReadOnlyList<UserSheetRow> rows;
            try
            {
                rows = spreadsheet.Read(file.Content, format.Value);
            }
            catch (InvalidDataException exception)
            {
                return Error.Validation(exception.Message);
            }

            if (rows.Count == 0)
            {
                return Error.Validation(Texts.T("Файл не містить жодного рядка з даними."));
            }

            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Import, cancellationToken);
            var units = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.Code }).ToDictionaryAsync(static u => u.Code, static u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var roles = await db.Roles.AsNoTracking().Include(static r => r.Permissions).ToDictionaryAsync(static r => r.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var errors = new List<ImportRowError>();
            var plans = new List<ImportPlan>();

            foreach (var group in rows.GroupBy(static r => r.Email.Trim().ToLowerInvariant()))
            {
                var first = group.First();
                var email = EmailAddress.Normalize(first.Email);
                if (email is null)
                {
                    errors.Add(new ImportRowError(first.RowNumber, first.Email, Texts.T("Некоректна електронна пошта.")));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(first.LastName) || string.IsNullOrWhiteSpace(first.FirstName))
                {
                    errors.Add(new ImportRowError(first.RowNumber, email, Texts.T("Прізвище та ім'я є обов'язковими.")));
                    continue;
                }

                var positions = new List<PositionInput>();
                var positionRows = group.Where(static r => !string.IsNullOrWhiteSpace(r.UnitCode) || !string.IsNullOrWhiteSpace(r.RoleCode)).ToList();
                foreach (var row in positionRows)
                {
                    if (!units.TryGetValue(row.UnitCode?.Trim() ?? string.Empty, out var unitId))
                    {
                        errors.Add(new ImportRowError(row.RowNumber, email, Texts.T("Підрозділ із кодом «{0}» не знайдено.", row.UnitCode)));
                        continue;
                    }

                    if (!scope.Contains(unitId))
                    {
                        errors.Add(new ImportRowError(row.RowNumber, email, Texts.T("Підрозділ «{0}» належить організації, якою Ви не керуєте: імпорт призначає посади лише у Ваших організаціях.", row.UnitCode)));
                        continue;
                    }

                    if (!roles.TryGetValue(row.RoleCode?.Trim() ?? string.Empty, out var role))
                    {
                        errors.Add(new ImportRowError(row.RowNumber, email, Texts.T("Роль із кодом «{0}» не знайдено.", row.RoleCode)));
                        continue;
                    }

                    if (!directory.CanAssign(role))
                    {
                        errors.Add(new ImportRowError(row.RowNumber, email, Texts.T("Ви не можете призначати роль «{0}».", Texts.T(role.Name))));
                        continue;
                    }

                    positions.Add(new PositionInput(unitId, role.Id, row.PositionTitle, positions.Count == 0));
                }

                var isActive = !BlockedStatuses.Contains(first.Status?.Trim().ToLowerInvariant());
                plans.Add(new ImportPlan(first.RowNumber, email, first, positions, positionRows.Count > 0, isActive));
            }

            var emails = plans.Select(static p => p.Email.ToUpperInvariant()).ToList();
            var existing = await db.Users
                .Include(static u => u.Positions)
                .Where(u => emails.Contains(u.NormalizedEmail!))
                .ToDictionaryAsync(static u => u.Email!, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var plan in plans)
            {
                if (!existing.TryGetValue(plan.Email, out var user))
                {
                    if (!scope.IsCabinetWide && !plan.HasPositionRows)
                    {
                        errors.Add(new ImportRowError(plan.Row, plan.Email, Texts.T("Вкажіть код підрозділу й код ролі: обліковий запис без посад бачить лише адміністратор усіх організацій.")));
                    }

                    continue;
                }

                // Import replaces the personal data and positions, so it may change only accounts that belong entirely to
                // the importer's organizations.
                if (!scope.Covers([.. user.Positions.Select(static p => p.OrganizationUnitId)]))
                {
                    errors.Add(new ImportRowError(plan.Row, plan.Email, Texts.T("Обліковий запис із цією поштою вже існує й має посади в організаціях, якими Ви не керуєте, тому імпорт не може його змінювати.")));
                    continue;
                }

                if (user.Id == currentUser.Id)
                {
                    if (!plan.IsActive)
                    {
                        errors.Add(new ImportRowError(plan.Row, plan.Email, Texts.T("Ви не можете заблокувати власний обліковий запис.")));
                    }

                    continue;
                }

                // The same hierarchy rule as for manual editing: only accounts whose permissions the importer also has.
                var target = await permissions.GetEffectivePermissionsAsync(user.Id, cancellationToken);
                if (target.Any(p => !currentUser.Has(p)))
                {
                    errors.Add(new ImportRowError(plan.Row, plan.Email, Texts.T("Цей обліковий запис має більше прав, ніж Ви, тому імпорт не може його змінювати.")));
                }
            }

            var toCreate = plans.Count(p => !existing.ContainsKey(p.Email));
            var toUpdate = plans.Count - toCreate;
            if (errors.Count > 0 || request.DryRun)
            {
                return new UserImportReport(rows.Count, toCreate, toUpdate, Applied: false, [.. errors.OrderBy(static e => e.Row)], 0);
            }

            try
            {
                var invited = await db.InTransactionAsync<List<User>>(
                    async ct =>
                    {
                        var issued = new List<User>();
                        foreach (var plan in plans)
                        {
                            var data = plan.Data;
                            if (existing.TryGetValue(plan.Email, out var user))
                            {
                                user.UpdatePersonalData(data.LastName, data.FirstName, data.MiddleName, data.PhoneNumber ?? user.PhoneNumber);
                                if (plan.Positions.Count > 0)
                                {
                                    user.SetPositions(plan.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
                                }

                                var blocked = user.IsActive && !plan.IsActive;
                                user.SetActive(plan.IsActive);
                                var updated = await identity.UpdateAsync(user);
                                if (!updated.IsSuccess)
                                {
                                    throw new DomainException($"{plan.Email}: {updated.Error.Message}");
                                }

                                // A blocked account loses its sessions immediately, as with manual blocking.
                                if (blocked)
                                {
                                    await identity.InvalidateSessionsAsync(user);
                                }
                            }
                            else
                            {
                                var created = new User(plan.Email, data.LastName, data.FirstName, data.MiddleName);
                                created.UpdatePersonalData(data.LastName, data.FirstName, data.MiddleName, data.PhoneNumber);
                                created.SetPositions(plan.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
                                created.SetActive(plan.IsActive);
                                var result = await identity.CreateAsync(created);
                                if (!result.IsSuccess)
                                {
                                    throw new DomainException($"{plan.Email}: {result.Error.Message}");
                                }

                                issued.Add(created);
                            }
                        }

                        audit.Add(AuditActions.UsersImported, details: $"Імпорт «{file.FileName}»: створено {toCreate}, оновлено {toUpdate}");
                        await db.SaveChangesAsync(ct);
                        return issued;
                    },
                    cancellationToken);

                // The invitations go only after the transaction is committed.
                foreach (var user in invited.Where(static u => u.IsActive))
                {
                    directory.Invite(user);
                }

                return new UserImportReport(rows.Count, toCreate, toUpdate, Applied: true, [], directory.CanSendEmail ? invited.Count(static u => u.IsActive) : 0);
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
            }
        }
    }

    /// <summary>
    /// <c>POST /api/v1/user-imports</c> (multipart) and <c>GET /api/v1/user-imports/roles</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var imports = UserDirectory.MapUserImports(api);
            imports.MapPost(string.Empty, HandleAsync).WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxImportSize + (64 * 1024)));
            imports.MapGet("roles", RolesAsync);
        }

        /// <summary>
        /// Imports users. Nothing is saved when any row is invalid.
        /// </summary>
        /// <param name="file">The XLSX or CSV file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="dryRun">Whether the file is only validated.</param>
        /// <returns>201 with the report when applied, 200 with the report otherwise.</returns>
        private static async Task<Results<Created<UserImportReport>, Ok<UserImportReport>, ProblemHttpResult>> HandleAsync(
            IFormFile file,
            IRequestHandler<Command, Result<UserImportReport>> handler,
            CancellationToken cancellationToken,
            bool dryRun = false)
        {
            await using var content = file.OpenReadStream();
            var result = await handler.HandleAsync(new Command(new FileUpload(content, file.FileName, file.ContentType, file.Length), dryRun), cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.ToProblem();
            }

            return result.Value.Applied ? TypedResults.Created((string?)null, result.Value) : TypedResults.Ok(result.Value);
        }

        /// <summary>
        /// Lists the role codes the import file may use.
        /// </summary>
        /// <param name="roles">Lists the roles.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The roles.</returns>
        private static async Task<Ok<IReadOnlyList<RoleOption>>> RolesAsync(IRequestHandler<ListRoleOptions.Query, IReadOnlyList<RoleOption>> roles, CancellationToken cancellationToken)
            => TypedResults.Ok(await roles.HandleAsync(new ListRoleOptions.Query(), cancellationToken));
    }

    /// <summary>
    /// A validated import group for a single e-mail.
    /// </summary>
    /// <param name="Row">The first row number.</param>
    /// <param name="Email">The normalized e-mail.</param>
    /// <param name="Data">The first row with the personal data.</param>
    /// <param name="Positions">The valid positions.</param>
    /// <param name="HasPositionRows">Whether the file has any position rows for the e-mail.</param>
    /// <param name="IsActive">The account state.</param>
    private sealed record ImportPlan(int Row, string Email, UserSheetRow Data, List<PositionInput> Positions, bool HasPositionRows, bool IsActive);
}
