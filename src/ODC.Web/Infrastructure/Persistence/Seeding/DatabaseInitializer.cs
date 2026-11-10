using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Features.News;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;

namespace ODC.Web.Infrastructure.Persistence.Seeding;

/// <summary>
/// Applies the migrations of the master database and of every organization's database and creates the mandatory initial
/// data: predefined roles and the default unit types (master database), the first organization (the registry and its own
/// database) and the first administrator with the position there. Optionally creates demo data: a second organization
/// (the charity fund), the structures, staff and news. Brings the organizations' copies of the members' accounts
/// (<see cref="Member"/>) up to date on every start.
/// </summary>
/// <param name="db">The master database.</param>
/// <param name="provisioning">Creates organizations.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="scopes">Runs work in an organization.</param>
/// <param name="memberships">The memberships of accounts.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="options">The seed options.</param>
/// <param name="emailOptions">The e-mail settings (the address of the activation page).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class DatabaseInitializer(
    MasterDbContext db,
    IOrganizationProvisioning provisioning,
    OrganizationRegistry registry,
    OrganizationScopes scopes,
    Memberships memberships,
    UserManager<User> userManager,
    IOptions<SeedOptions> options,
    IOptions<EmailOptions> emailOptions,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
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
        ("director@odc.local", "Шевченко", "Олександр", "Іванович", null, SystemRoles.Head, "Генеральний директор"),
        ("deputy@odc.local", "Мельник", "Наталія", "Петрівна", "MGMT", SystemRoles.DeputyHead, "Заступник генерального директора"),
        ("head.it@odc.local", "Бондаренко", "Андрій", "Сергійович", "IT", SystemRoles.HeadOfUnit, "Директор департаменту ІТ"),
        ("head.fin@odc.local", "Ткаченко", "Ірина", "Володимирівна", "FIN", SystemRoles.HeadOfUnit, "Фінансовий директор"),
        ("head.dev@odc.local", "Кравченко", "Дмитро", "Олегович", "DEV", SystemRoles.HeadOfUnit, "Начальник відділу розробки"),
        ("engineer@odc.local", "Коваленко", "Марія", "Андріївна", "DEV-WEB", SystemRoles.Employee, "Провідний інженер-програміст"),
        ("clerk@odc.local", "Шевчук", "Тетяна", "Іванівна", "OFFICE", SystemRoles.RecordsClerk, "Діловод"),
        ("hr@odc.local", "Поліщук", "Оксана", "Василівна", "HR", SystemRoles.HrSpecialist, "Інспектор з кадрів"),
        ("press@odc.local", "Лисенко", "Юлія", "Олександрівна", "COMM", SystemRoles.Communications, "Менеджер з комунікацій"),
        ("lviv@odc.local", "Гнатюк", "Роман", "Степанович", "LVIV", SystemRoles.HeadOfUnit, "Директор філії"),
        ("external@odc.local", "Гончаренко", "Максим", "Ігорович", null, SystemRoles.External, "Представник контрагента"),
        ("fund@odc.local", "Олійник", "Світлана", "Миколаївна", "FUND", SystemRoles.Head, "Голова правління"),
        ("hr.fund@odc.local", "Савчук", "Ольга", "Петрівна", "FUND-BOARD", SystemRoles.HrSpecialist, "Менеджер з персоналу"),
        ("projects.fund@odc.local", "Руденко", "Віктор", "Андрійович", "FUND-PROJ", SystemRoles.Employee, "Координатор проєктів"),
    ];

    /// <summary>
    /// Applies migrations and seeds the data.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the database is ready.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);

        var roles = await SeedRolesAsync(cancellationToken);
        var types = await SeedUnitTypesAsync(cancellationToken);
        var root = await SeedRootAsync(types, cancellationToken);
        await PrepareOrganizationsAsync(cancellationToken);
        await SeedAdministratorAsync(root, roles[SystemRoles.Administrator], cancellationToken);

        if (options.Value.DemoData)
        {
            var fund = await SeedDemoOrganizationAsync(types, cancellationToken);
            var units = await SeedDemoUnitsAsync(root, fund, types, cancellationToken);
            await SeedDemoStaffAsync(root, units, roles, cancellationToken);
            await SeedDemoNewsAsync(root, fund, cancellationToken);
        }

        // Other organizations list the news of the whole cabinet through the index of the master database.
        await GeneralNews.RebuildAsync(db, await registry.AllAsync(cancellationToken), scopes, cancellationToken);
    }

    /// <summary>
    /// Logs that the first administrator was created and where it is activated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="email">The administrator e-mail.</param>
    /// <param name="activationUrl">The address of the activation page.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Created the first administrator {Email}. Activate the account at {ActivationUrl}: a one-time code goes to this e-mail")]
    private static partial void LogAdministratorCreated(ILogger logger, string email, string activationUrl);

    /// <summary>
    /// Logs that demo data was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of demo accounts.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} demo accounts")]
    private static partial void LogDemoCreated(ILogger logger, int count);

    /// <summary>
    /// Loads the tracked member of an account with its positions, copying the account when the organization does not
    /// know the person yet.
    /// </summary>
    /// <param name="target">The organization's database.</param>
    /// <param name="user">The account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The member.</returns>
    private static async Task<Member> MemberOfAsync(OrganizationDbContext target, User user, CancellationToken cancellationToken)
    {
        var member = await target.Members.Include(static m => m.Positions).FirstOrDefaultAsync(m => m.Id == user.Id, cancellationToken);
        if (member is null)
        {
            member = new Member(MemberProfile.Of(user));
            target.Members.Add(member);
        }

        return member;
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
    /// Creates the first organization (with its database) when the registry is empty and returns the oldest one.
    /// </summary>
    /// <param name="types">The unit types by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first organization.</returns>
    private async Task<Organization> SeedRootAsync(Dictionary<string, UnitType> types, CancellationToken cancellationToken)
    {
        if (await registry.AllAsync(cancellationToken) is { Count: > 0 } existing)
        {
            return existing[0];
        }

        var type = types.Values.Where(static t => t.IsOrganization).MinBy(static t => t.SortOrder);
        if (type is null)
        {
            type = new UnitType("ORGANIZATION", "Організація", isOrganization: true);
            db.UnitTypes.Add(type);
            types[type.Code] = type;
            await db.SaveChangesAsync(cancellationToken);
        }

        var settings = options.Value;
        return await CreateOrganizationAsync(settings.OrganizationCode, settings.OrganizationName, settings.OrganizationShortName, type.Id, 0, cancellationToken);
    }

    /// <summary>
    /// Creates an organization with its database and returns it.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="name">The name.</param>
    /// <param name="shortName">The abbreviation.</param>
    /// <param name="typeId">The unit type of the root.</param>
    /// <param name="sortOrder">The display order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization.</returns>
    private async Task<Organization> CreateOrganizationAsync(string code, string name, string? shortName, Guid typeId, int sortOrder, CancellationToken cancellationToken)
    {
        var created = await provisioning.CreateAsync(code, name, shortName, typeId, sortOrder, cancellationToken);
        if (!created.IsSuccess)
        {
            throw new InvalidOperationException($"Cannot create the organization {code}: {created.Error.Message}");
        }

        registry.Invalidate();
        return await registry.FindAsync(created.Value, cancellationToken)
            ?? throw new InvalidOperationException($"The organization {code} is not in the registry.");
    }

    /// <summary>
    /// Brings every organization's database to the latest migration and repairs its copies of the members' accounts.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the databases are ready.</returns>
    private async Task PrepareOrganizationsAsync(CancellationToken cancellationToken)
    {
        foreach (var organization in await registry.AllAsync(cancellationToken))
        {
            var people = await db.Memberships.Where(m => m.OrganizationId == organization.Id).Select(static m => m.UserId).ToListAsync(cancellationToken);
            var profiles = (await db.Users.AsNoTracking().Where(u => people.Contains(u.Id)).ToListAsync(cancellationToken)).Select(MemberProfile.Of).ToList();
            await scopes.RunAsync(organization, async services =>
            {
                var target = services.GetRequiredService<OrganizationDbContext>();
                await target.Database.MigrateAsync(cancellationToken);
                await MemberSync.CopyAsync(target, profiles, cancellationToken);
            });
        }
    }

    /// <summary>
    /// Creates the first administrator when the first organization has none.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="adminRole">The administrator role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the administrator exists.</returns>
    private async Task SeedAdministratorAsync(Organization root, Role adminRole, CancellationToken cancellationToken)
    {
        var exists = await scopes.RunAsync(root, services => services.GetRequiredService<OrganizationDbContext>().StaffPositions.AnyAsync(p => p.RoleId == adminRole.Id, cancellationToken));
        if (exists)
        {
            return;
        }

        var settings = options.Value;
        var admin = await userManager.FindByEmailAsync(settings.AdminEmail);
        if (admin is null)
        {
            admin = new User(settings.AdminEmail, "Адміністратор", "Системний", null);
            var result = await userManager.CreateAsync(admin);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Cannot create the administrator: {string.Join(" ", result.Errors.Select(static e => e.Description))}");
            }

            // No password exists anywhere: the administrator activates the account by e-mail like every other user.
            LogAdministratorCreated(logger, settings.AdminEmail, emailOptions.Value.SiteUrl.TrimEnd('/') + AccountEmails.ActivationPath);
        }

        await PlaceAsync(root, admin, [(root.Id, adminRole.Id, "Системний адміністратор", true)], cancellationToken);
    }

    /// <summary>
    /// Gives a person positions in an organization and records the membership.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="user">The account.</param>
    /// <param name="positions">The positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    private async Task PlaceAsync(Organization organization, User user, List<(Guid UnitId, Guid RoleId, string? Title, bool IsPrimary)> positions, CancellationToken cancellationToken)
    {
        await scopes.RunAsync(organization, async services =>
        {
            var target = services.GetRequiredService<OrganizationDbContext>();
            (await MemberOfAsync(target, user, cancellationToken)).SetPositions(positions);
            await target.SaveChangesAsync(cancellationToken);
        });
        await memberships.SyncAsync(organization.Id, [(user.Id, true)], cancellationToken);
    }

    /// <summary>
    /// Creates the second demo organization (the charity fund) with its database.
    /// </summary>
    /// <param name="types">The unit types by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fund, or null when its unit type was removed.</returns>
    private async Task<Organization?> SeedDemoOrganizationAsync(Dictionary<string, UnitType> types, CancellationToken cancellationToken)
    {
        var (code, name, shortName, typeCode, _) = DemoUnits.First(static u => u.Parent is { Length: 0 });
        if (await registry.FindByCodeAsync(code, cancellationToken) is { } existing)
        {
            return existing;
        }

        return types.TryGetValue(typeCode, out var type) ? await CreateOrganizationAsync(code, name, shortName, type.Id, 1, cancellationToken) : null;
    }

    /// <summary>
    /// Creates the demo structures: the units of the first organization and of the fund, each in its database.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="fund">The fund, if it exists.</param>
    /// <param name="types">The unit types by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units by code with their organizations.</returns>
    private async Task<Dictionary<string, (Organization Organization, Guid UnitId)>> SeedDemoUnitsAsync(Organization root, Organization? fund, Dictionary<string, UnitType> types, CancellationToken cancellationToken)
    {
        var placed = new Dictionary<string, (Organization Organization, Guid UnitId)>(StringComparer.OrdinalIgnoreCase);
        if (fund is not null)
        {
            placed[fund.Code] = (fund, fund.Id);
        }

        foreach (var organization in fund is null ? [root] : new[] { root, fund })
        {
            await scopes.RunAsync(organization, async services =>
            {
                var target = services.GetRequiredService<OrganizationDbContext>();
                var units = await target.OrganizationUnits.ToDictionaryAsync(static u => u.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
                var order = 0;
                foreach (var (code, name, shortName, typeCode, parent) in DemoUnits)
                {
                    // A unit belongs to the fund when its parent does; the fund itself is an organization of its own.
                    var inFund = parent is { Length: > 0 } && fund is not null && placed.TryGetValue(parent, out var owner) && owner.Organization.Id == fund.Id;
                    if (parent is { Length: 0 } || inFund != (organization.Id == fund?.Id))
                    {
                        continue;
                    }

                    if (!units.TryGetValue(code, out var unit))
                    {
                        // Demo units need the default types; those the administrator has removed are skipped.
                        if (!types.TryGetValue(typeCode, out var type))
                        {
                            continue;
                        }

                        var parentId = parent is null ? organization.Id : units.TryGetValue(parent, out var parentUnit) ? parentUnit.Id : organization.Id;
                        unit = new OrganizationUnit(code, name, type.Id, parentId);
                        unit.Update(code, name, shortName, type.Id, ++order);
                        target.OrganizationUnits.Add(unit);
                        units[code] = unit;
                    }

                    placed[code] = (organization, unit.Id);
                }

                await target.SaveChangesAsync(cancellationToken);
            });
        }

        return placed;
    }

    /// <summary>
    /// Creates demo staff accounts with a known password (they still enroll an authenticator at the first sign-in), each
    /// with positions in their organization.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="units">The units by code with their organizations.</param>
    /// <param name="roles">The roles by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the accounts exist.</returns>
    private async Task SeedDemoStaffAsync(Organization root, Dictionary<string, (Organization Organization, Guid UnitId)> units, Dictionary<string, Role> roles, CancellationToken cancellationToken)
    {
        var password = options.Value.DemoPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var created = 0;
        foreach (var (email, last, first, middle, unitCode, role, title) in DemoStaff)
        {
            (Organization Organization, Guid UnitId) place = unitCode is null ? (root, root.Id) : units.GetValueOrDefault(unitCode);
            if (place.Organization is null || await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new User(email, last, first, middle);
            if (!(await userManager.CreateAsync(user, password)).Succeeded)
            {
                continue;
            }

            List<(Guid, Guid, string?, bool)> positions = [(place.UnitId, roles[role].Id, title, true)];
            if (email == "head.dev@odc.local" && units.TryGetValue("DEV-WEB", out var sector) && sector.Organization.Id == place.Organization.Id)
            {
                // A second, part-time position shows how one person can work in several units.
                positions.Add((sector.UnitId, roles[SystemRoles.Employee].Id, "Архітектор програмного забезпечення", false));
            }

            await PlaceAsync(place.Organization, user, positions, cancellationToken);
            created++;
        }

        if (created > 0)
        {
            LogDemoCreated(logger, created);
        }
    }

    /// <summary>
    /// Creates demo news in each organization; the news of the whole cabinet goes to the first one for now.
    /// </summary>
    /// <param name="root">The first organization.</param>
    /// <param name="fund">The fund, if it exists.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the news exist.</returns>
    private async Task SeedDemoNewsAsync(Organization root, Organization? fund, CancellationToken cancellationToken)
    {
        (string Slug, string Title, string Summary, string Body, NewsCategory Category, bool Pinned, int DaysAgo, Organization Owner, bool ForCabinet)[] items =
        [
            (
                "zapusk-kabinetu-orhanizatsiinykh-dokumentiv",
                "Запущено Кабінет організаційних документів",
                "Організація переходить на електронний документообіг із кваліфікованим електронним підписом.",
                "## Що змінюється\n\nВідтепер накази, розпорядження, службові записки, договори та акти погоджуються **в електронному вигляді**.\n\n- підпис накладається у браузері — ключ і пароль не залишають Ваш пристрій;\n- підтримуються формати CAdES, XAdES, PAdES, ASiC-S та ASiC-E;\n- вхід захищено двофакторною автентифікацією.\n\nПитання щодо доступу — до відділу кадрів або системного адміністратора.",
                NewsCategory.Announcement,
                true,
                1,
                root,
                true),
            (
                "novyi-poriadok-pohodzhennia-dohovoriv",
                "Оновлено порядок погодження договорів",
                "Договори погоджуються паралельним маршрутом: юридичний відділ, фінансовий департамент і керівник підрозділу.",
                "Ініціатор створює документ виду **«Договір»**, обирає свій підрозділ і додає погоджувачів до маршруту.\n\n> Строк погодження — до трьох робочих днів.",
                NewsCategory.News,
                false,
                3,
                root,
                false),
            (
                "navchannia-elektronnyi-pidpys",
                "Навчання: робота з кваліфікованим електронним підписом",
                "Запрошуємо працівників на онлайн-заняття з підписання та перевірки документів.",
                "Розглянемо файлові ключі, формати CAdES, XAdES, PAdES і ASiC та перевірку підпису.\n\nРеєстрація — у відділі кадрів.",
                NewsCategory.Training,
                false,
                6,
                root,
                false),
            (
                "fond-shukaie-volonteriv",
                "Фонд «Відкриті серця» запрошує волонтерів",
                "Відділ проєктів набирає волонтерів для зимової благодійної кампанії.",
                "Потрібні координатори збору допомоги та водії.\n\nЗаявки приймає **відділ проєктів** фонду до кінця місяця.",
                NewsCategory.Projects,
                false,
                2,
                fund ?? root,
                false),
        ];

        var author = await db.Users.OrderBy(static u => u.CreatedAt).FirstAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var group in items.GroupBy(static i => i.Owner.Id))
        {
            var owner = group.First().Owner;
            await scopes.RunAsync(owner, async services =>
            {
                var target = services.GetRequiredService<OrganizationDbContext>();
                if (await target.NewsArticles.AnyAsync(cancellationToken))
                {
                    return;
                }

                // The author must be known to the organization's database (a participant from another organization).
                await MemberOfAsync(target, author, cancellationToken);
                foreach (var item in group)
                {
                    var article = new NewsArticle(item.Slug, item.Title, item.Summary, item.Body, NewsBodyFormat.Markdown, item.Category, author.Id);
                    article.Pin(item.Pinned);
                    article.AssignTo(item.ForCabinet ? null : owner.Id);
                    article.Publish(now.AddDays(-item.DaysAgo));
                    target.NewsArticles.Add(article);
                }

                await target.SaveChangesAsync(cancellationToken);
            });
        }
    }
}
