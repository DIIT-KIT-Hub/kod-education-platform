using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Domain.Organization;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Persistence.Seeding;

/// <summary>
/// Applies migrations and creates the mandatory initial data: predefined roles, the default unit types, the first organization and the first administrator.
/// Optionally creates a demo structure, demo staff and news.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="identity">The identity service.</param>
/// <param name="tokenProtector">Encrypts secret user tokens.</param>
/// <param name="environment">The host environment.</param>
/// <param name="options">The seed options.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class DatabaseInitializer(
    ApplicationDbContext db,
    UserManager<User> userManager,
    IIdentityService identity,
    UserTokenProtector tokenProtector,
    IHostEnvironment environment,
    IOptions<SeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
    /// <summary>
    /// The file that receives a generated password of the first administrator (never the log).
    /// </summary>
    private const string AdminPasswordFile = "App_Data/initial-admin-password.txt";

    /// <summary>
    /// The default unit types created for an empty catalog: code, name, organization flag, icon, description.
    /// </summary>
    private static readonly (string Code, string Name, bool IsOrganization, string Icon, string Description)[] DefaultUnitTypes =
    [
        ("ORGANIZATION", "Організація", true, "building-2", "Самостійна організація: компанія, установа, заклад, фонд, громадська організація."),
        ("DEPARTMENT", "Департамент", false, "layers", "Великий підрозділ, що об'єднує управління, відділи чи служби."),
        ("DIRECTORATE", "Управління", false, "briefcase", "Підрозділ, що об'єднує кілька відділів або керує напрямом діяльності."),
        ("DIVISION", "Відділ", false, "users", "Базовий структурний підрозділ."),
        ("SECTOR", "Сектор", false, "folder-tree", "Невеликий підрозділ у складі відділу."),
        ("SERVICE", "Служба", false, "settings", "Функціональна служба: канцелярія, підтримка, охорона праці тощо."),
        ("TEAM", "Група / команда", false, "network", "Робоча або проєктна група."),
        ("BRANCH", "Філія / представництво", false, "map-pin", "Відокремлений підрозділ організації в іншому місці."),
    ];

    /// <summary>
    /// Demo units: code, name, short name, type code, parent code (<see langword="null"/> — the first organization, empty — top level).
    /// </summary>
    private static readonly (string Code, string Name, string? Short, string Type, string? Parent)[] DemoUnits =
    [
        ("MGMT", "Дирекція", null, "DIRECTORATE", null),
        ("OFFICE", "Канцелярія", null, "SERVICE", null),
        ("HR", "Відділ кадрів", "ВК", "DIVISION", null),
        ("FIN", "Фінансовий департамент", "ФД", "DEPARTMENT", null),
        ("ACC", "Бухгалтерія", null, "DIVISION", "FIN"),
        ("PLAN", "Відділ фінансового планування", null, "DIVISION", "FIN"),
        ("IT", "Департамент інформаційних технологій", "ДІТ", "DEPARTMENT", null),
        ("DEV", "Відділ розробки", null, "DIVISION", "IT"),
        ("DEV-WEB", "Сектор вебзастосунків", null, "SECTOR", "DEV"),
        ("SUPPORT", "Служба підтримки", null, "SERVICE", "IT"),
        ("LEGAL", "Юридичний відділ", "ЮВ", "DIVISION", null),
        ("COMM", "Відділ комунікацій", null, "DIVISION", null),
        ("LVIV", "Львівська філія", null, "BRANCH", null),
        ("SALES", "Відділ продажу", null, "DIVISION", "LVIV"),
        ("FUND", "Благодійний фонд «Відкриті серця»", "БФ «ВС»", "ORGANIZATION", string.Empty),
        ("FUND-BOARD", "Правління", null, "DIRECTORATE", "FUND"),
        ("FUND-PROJ", "Відділ проєктів", null, "DIVISION", "FUND"),
    ];

    /// <summary>
    /// Demo staff: e-mail, last name, first name, patronymic, unit code (<see langword="null"/> — the first organization), role code, title.
    /// </summary>
    private static readonly (string Email, string Last, string First, string Middle, string? Unit, string Role, string Title)[] DemoStaff =
    [
        ("director@kod.local", "Шевченко", "Олександр", "Іванович", null, SystemRoles.Head, "Генеральний директор"),
        ("deputy@kod.local", "Мельник", "Наталія", "Петрівна", "MGMT", SystemRoles.DeputyHead, "Заступник генерального директора"),
        ("head.it@kod.local", "Бондаренко", "Андрій", "Сергійович", "IT", SystemRoles.HeadOfUnit, "Директор департаменту ІТ"),
        ("head.fin@kod.local", "Ткаченко", "Ірина", "Володимирівна", "FIN", SystemRoles.HeadOfUnit, "Фінансовий директор"),
        ("head.dev@kod.local", "Кравченко", "Дмитро", "Олегович", "DEV", SystemRoles.HeadOfUnit, "Начальник відділу розробки"),
        ("engineer@kod.local", "Коваленко", "Марія", "Андріївна", "DEV-WEB", SystemRoles.Employee, "Провідний інженер-програміст"),
        ("clerk@kod.local", "Шевчук", "Тетяна", "Іванівна", "OFFICE", SystemRoles.RecordsClerk, "Діловод"),
        ("hr@kod.local", "Поліщук", "Оксана", "Василівна", "HR", SystemRoles.HrSpecialist, "Інспектор з кадрів"),
        ("press@kod.local", "Лисенко", "Юлія", "Олександрівна", "COMM", SystemRoles.Communications, "Менеджер з комунікацій"),
        ("lviv@kod.local", "Гнатюк", "Роман", "Степанович", "LVIV", SystemRoles.HeadOfUnit, "Директор філії"),
        ("external@kod.local", "Гончаренко", "Максим", "Ігорович", null, SystemRoles.External, "Представник контрагента"),
        ("fund@kod.local", "Олійник", "Світлана", "Миколаївна", "FUND", SystemRoles.Head, "Голова правління"),
        ("hr.fund@kod.local", "Савчук", "Ольга", "Петрівна", "FUND-BOARD", SystemRoles.HrSpecialist, "Менеджер з персоналу"),
        ("projects.fund@kod.local", "Руденко", "Віктор", "Андрійович", "FUND-PROJ", SystemRoles.Employee, "Координатор проєктів"),
    ];

    /// <summary>
    /// Applies migrations and seeds the data.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the database is ready.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await ProtectLegacyTokensAsync(cancellationToken);

        var roles = await SeedRolesAsync(cancellationToken);
        var types = await SeedUnitTypesAsync(cancellationToken);
        var root = await SeedRootAsync(types, cancellationToken);
        await SeedAdministratorAsync(root, roles[SystemRoles.Administrator], cancellationToken);

        if (options.Value.DemoData)
        {
            var units = await SeedDemoUnitsAsync(root, types, cancellationToken);
            await SeedDemoStaffAsync(root, units, roles, cancellationToken);
            await SeedDemoNewsAsync(root, units, cancellationToken);
        }
    }

    /// <summary>
    /// Logs where a generated administrator password was written.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="email">The administrator e-mail.</param>
    /// <param name="path">The file with the temporary password.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Created the first administrator {Email}; the temporary password is in {Path}. Sign in, set your own password and delete the file.")]
    private static partial void LogAdministratorCreated(ILogger logger, string email, string path);

    /// <summary>
    /// Logs encrypted legacy tokens.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of tokens.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Encrypted {Count} authenticator keys and recovery code sets stored before encryption was enabled")]
    private static partial void LogTokensProtected(ILogger logger, int count);

    /// <summary>
    /// Logs that demo data was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of demo accounts.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} demo accounts")]
    private static partial void LogDemoCreated(ILogger logger, int count);

    /// <summary>
    /// Encrypts authenticator keys and recovery codes that were stored in plain text by earlier versions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every secret token is encrypted.</returns>
    private async Task ProtectLegacyTokensAsync(CancellationToken cancellationToken)
    {
        var secrets = UserTokenProtector.Secrets.ToList();
        var legacy = await db.UserTokens
            .Where(t => t.LoginProvider == UserTokenProtector.IdentityProvider && secrets.Contains(t.Name) && t.Value != null && !t.Value.StartsWith("dp1:"))
            .ToListAsync(cancellationToken);
        if (legacy.Count == 0)
        {
            return;
        }

        foreach (var token in legacy)
        {
            token.Value = tokenProtector.Protect(token.UserId, token.LoginProvider, token.Name, token.Value);
        }

        await db.SaveChangesAsync(cancellationToken);
        LogTokensProtected(logger, legacy.Count);
    }

    /// <summary>
    /// Writes a generated administrator password to a file readable only by the service account.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full path of the file.</returns>
    private async Task<string> WriteAdminPasswordAsync(string password, CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(AdminPasswordFile, environment.ContentRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, password + Environment.NewLine, cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return path;
    }

    /// <summary>
    /// Creates missing predefined roles; keeps administrator-made changes of existing ones.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles by code.</returns>
    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Roles.Include(static r => r.Permissions).ToDictionaryAsync(static r => r.Code, cancellationToken);
        foreach (var definition in SystemRoles.All)
        {
            if (!existing.TryGetValue(definition.Code, out var role))
            {
                role = new Role(definition.Code, definition.SourceName, definition.SourceDescription, isSystem: true);
                role.Update(definition.SourceName, definition.SourceDescription, definition.IsLeadership);
                role.SetPermissions(definition.Permissions);
                db.Roles.Add(role);
                existing[definition.Code] = role;
            }
            else if (role.IsAdministrator)
            {
                // New permissions added in later releases are always granted to administrators.
                role.SetPermissions(Permissions.Codes);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Fills an empty unit type catalog with types common to most organizations; later changes of the administrator are kept.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The types by code.</returns>
    private async Task<Dictionary<string, UnitType>> SeedUnitTypesAsync(CancellationToken cancellationToken)
    {
        var existing = await db.UnitTypes.ToDictionaryAsync(static t => t.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        if (existing.Count > 0)
        {
            return existing;
        }

        var order = 0;
        foreach (var (code, name, isOrganization, icon, description) in DefaultUnitTypes)
        {
            var type = new UnitType(code, name, isOrganization);
            type.Update(code, name, description, icon, isOrganization, order += 10);
            db.UnitTypes.Add(type);
            existing[code] = type;
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Creates the first organization when the structure is empty and returns the first top-level organization.
    /// </summary>
    /// <param name="types">The unit types by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first organization.</returns>
    private async Task<OrganizationUnit> SeedRootAsync(Dictionary<string, UnitType> types, CancellationToken cancellationToken)
    {
        var root = await db.OrganizationUnits
            .Where(static u => u.ParentId == null)
            .OrderBy(static u => u.SortOrder)
            .ThenBy(static u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (root is not null)
        {
            return root;
        }

        var type = types.Values.Where(static t => t.IsOrganization).MinBy(static t => t.SortOrder);
        if (type is null)
        {
            type = new UnitType("ORGANIZATION", "Організація", isOrganization: true);
            db.UnitTypes.Add(type);
            types[type.Code] = type;
        }

        var settings = options.Value;
        root = new OrganizationUnit(settings.OrganizationCode, settings.OrganizationName, type.Id, null);
        root.Update(settings.OrganizationCode, settings.OrganizationName, settings.OrganizationShortName, type.Id, 0);
        db.OrganizationUnits.Add(root);
        await db.SaveChangesAsync(cancellationToken);
        return root;
    }

    /// <summary>
    /// Creates the first administrator when no administrator exists.
    /// </summary>
    /// <param name="root">The root unit.</param>
    /// <param name="adminRole">The administrator role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the administrator exists.</returns>
    private async Task SeedAdministratorAsync(OrganizationUnit root, Role adminRole, CancellationToken cancellationToken)
    {
        if (await db.StaffPositions.AnyAsync(p => p.RoleId == adminRole.Id, cancellationToken))
        {
            return;
        }

        var settings = options.Value;
        var password = string.IsNullOrWhiteSpace(settings.AdminPassword) ? identity.GenerateTemporaryPassword() : settings.AdminPassword;
        var admin = await userManager.FindByEmailAsync(settings.AdminEmail);
        if (admin is null)
        {
            admin = new User(settings.AdminEmail, "Адміністратор", "Системний", null);
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Cannot create the administrator: {string.Join(" ", result.Errors.Select(static e => e.Description))}");
            }

            if (string.IsNullOrWhiteSpace(settings.AdminPassword))
            {
                LogAdministratorCreated(logger, settings.AdminEmail, await WriteAdminPasswordAsync(password, cancellationToken));
            }
        }

        admin.SetPositions([(root.Id, adminRole.Id, "Системний адміністратор", true)]);
        await userManager.UpdateAsync(admin);
    }

    /// <summary>
    /// Creates the demo structure.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="types">The unit types by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units by code.</returns>
    private async Task<Dictionary<string, OrganizationUnit>> SeedDemoUnitsAsync(OrganizationUnit root, Dictionary<string, UnitType> types, CancellationToken cancellationToken)
    {
        var units = await db.OrganizationUnits.ToDictionaryAsync(static u => u.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var order = 0;
        foreach (var (code, name, shortName, typeCode, parent) in DemoUnits)
        {
            // Demo units need the default types; those the administrator has removed are skipped.
            if (units.ContainsKey(code) || !types.TryGetValue(typeCode, out var type))
            {
                continue;
            }

            Guid? parentId = parent switch
            {
                null => root.Id,
                { Length: 0 } => null,
                _ => units.TryGetValue(parent, out var parentUnit) ? parentUnit.Id : root.Id,
            };
            var unit = new OrganizationUnit(code, name, type.Id, parentId);
            unit.Update(code, name, shortName, type.Id, ++order);
            db.OrganizationUnits.Add(unit);
            units[code] = unit;
        }

        await db.SaveChangesAsync(cancellationToken);
        return units;
    }

    /// <summary>
    /// Creates demo staff accounts with a known password (they still enroll an authenticator at the first sign-in).
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="units">The units by code.</param>
    /// <param name="roles">The roles by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the accounts exist.</returns>
    private async Task SeedDemoStaffAsync(OrganizationUnit root, Dictionary<string, OrganizationUnit> units, Dictionary<string, Role> roles, CancellationToken cancellationToken)
    {
        var password = options.Value.DemoPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var created = 0;
        foreach (var (email, last, first, middle, unitCode, role, title) in DemoStaff)
        {
            var unit = unitCode is null ? root : units.GetValueOrDefault(unitCode);
            if (unit is null || await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new User(email, last, first, middle);
            user.RequirePasswordChange(false);
            user.SetPositions([(unit.Id, roles[role].Id, title, true)]);
            if (email == "head.dev@kod.local" && units.TryGetValue("DEV-WEB", out var sector))
            {
                // A second, part-time position shows how one person can work in several units.
                user.SetPositions([(unit.Id, roles[role].Id, title, true), (sector.Id, roles[SystemRoles.Employee].Id, "Архітектор програмного забезпечення", false)]);
            }

            var result = await userManager.CreateAsync(user, password);
            created += result.Succeeded ? 1 : 0;
        }

        if (created > 0)
        {
            LogDemoCreated(logger, created);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Creates demo news: one for the whole cabinet and the others for their organizations.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="units">The units by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the news exist.</returns>
    private async Task SeedDemoNewsAsync(OrganizationUnit root, Dictionary<string, OrganizationUnit> units, CancellationToken cancellationToken)
    {
        if (await db.NewsArticles.AnyAsync(cancellationToken))
        {
            return;
        }

        var author = await db.Users.OrderBy(static u => u.CreatedAt).FirstAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var fund = units.GetValueOrDefault("FUND")?.Id ?? root.Id;
        (string Slug, string Title, string Summary, string Body, NewsCategory Category, bool Pinned, int DaysAgo, Guid? Organization)[] items =
        [
            (
                "zapusk-kabinetu-orhanizatsiinykh-dokumentiv",
                "Запущено Кабінет організаційних документів",
                "Організація переходить на електронний документообіг із кваліфікованим електронним підписом.",
                "## Що змінюється\n\nВідтепер накази, розпорядження, службові записки, договори та акти погоджуються **в електронному вигляді**.\n\n- підпис накладається у браузері — ключ і пароль не залишають ваш пристрій;\n- підтримуються формати CAdES, XAdES, PAdES, ASiC-S та ASiC-E;\n- вхід захищено двофакторною автентифікацією.\n\nПитання щодо доступу — до відділу кадрів або системного адміністратора.",
                NewsCategory.Announcement,
                true,
                1,
                null),
            (
                "novyi-poriadok-pohodzhennia-dohovoriv",
                "Оновлено порядок погодження договорів",
                "Договори погоджуються паралельним маршрутом: юридичний відділ, фінансовий департамент і керівник підрозділу.",
                "Ініціатор створює документ виду **«Договір»**, обирає свій підрозділ і додає погоджувачів до маршруту.\n\n> Строк погодження — до трьох робочих днів.",
                NewsCategory.News,
                false,
                3,
                root.Id),
            (
                "navchannia-elektronnyi-pidpys",
                "Навчання: робота з кваліфікованим електронним підписом",
                "Запрошуємо працівників на онлайн-заняття з підписання та перевірки документів.",
                "Розглянемо файлові ключі, формати CAdES, XAdES, PAdES і ASiC та перевірку підпису.\n\nРеєстрація — у відділі кадрів.",
                NewsCategory.Training,
                false,
                6,
                root.Id),
            (
                "fond-shukaie-volonteriv",
                "Фонд «Відкриті серця» запрошує волонтерів",
                "Відділ проєктів набирає волонтерів для зимової благодійної кампанії.",
                "Потрібні координатори збору допомоги та водії.\n\nЗаявки приймає **відділ проєктів** фонду до кінця місяця.",
                NewsCategory.Projects,
                false,
                2,
                fund),
        ];

        foreach (var item in items)
        {
            var article = new NewsArticle(item.Slug, item.Title, item.Summary, item.Body, NewsBodyFormat.Markdown, item.Category, author.Id);
            article.Pin(item.Pinned);
            article.AssignTo(item.Organization);
            article.Publish(now.AddDays(-item.DaysAgo));
            db.NewsArticles.Add(article);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
