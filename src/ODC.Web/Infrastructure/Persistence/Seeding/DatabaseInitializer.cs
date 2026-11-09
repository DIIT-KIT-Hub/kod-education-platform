using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;

namespace ODC.Web.Infrastructure.Persistence.Seeding;

/// <summary>
/// Applies the migrations of both databases and creates the mandatory initial data: predefined roles and the default
/// unit types (master database), the first organization and the first administrator with the position there.
/// Optionally creates a demo structure, demo staff and news. Brings the organization's copies of the accounts
/// (<see cref="Member"/>) up to date on every start.
/// </summary>
/// <param name="db">The master database.</param>
/// <param name="organization">The organization's database.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="options">The seed options.</param>
/// <param name="emailOptions">The e-mail settings (the address of the activation page).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class DatabaseInitializer(
    MasterDbContext db,
    OrganizationDbContext organization,
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
        await organization.Database.MigrateAsync(cancellationToken);
        await MemberSync.CopyAsync(organization, [.. (await db.Users.AsNoTracking().ToListAsync(cancellationToken)).Select(MemberProfile.Of)], cancellationToken);

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
        var root = await organization.OrganizationUnits
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
            await db.SaveChangesAsync(cancellationToken);
        }

        var settings = options.Value;
        root = new OrganizationUnit(settings.OrganizationCode, settings.OrganizationName, type.Id, null);
        root.Update(settings.OrganizationCode, settings.OrganizationName, settings.OrganizationShortName, type.Id, 0);
        organization.OrganizationUnits.Add(root);
        await organization.SaveChangesAsync(cancellationToken);
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
        if (await organization.StaffPositions.AnyAsync(p => p.RoleId == adminRole.Id, cancellationToken))
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

        (await MemberOfAsync(admin, cancellationToken)).SetPositions([(root.Id, adminRole.Id, "Системний адміністратор", true)]);
        await organization.SaveChangesAsync(cancellationToken);
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
        var units = await organization.OrganizationUnits.ToDictionaryAsync(static u => u.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
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
            organization.OrganizationUnits.Add(unit);
            units[code] = unit;
        }

        await organization.SaveChangesAsync(cancellationToken);
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
            List<(Guid, Guid, string?, bool)> positions = [(unit.Id, roles[role].Id, title, true)];
            if (email == "head.dev@odc.local" && units.TryGetValue("DEV-WEB", out var sector))
            {
                // A second, part-time position shows how one person can work in several units.
                positions.Add((sector.Id, roles[SystemRoles.Employee].Id, "Архітектор програмного забезпечення", false));
            }

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                (await MemberOfAsync(user, cancellationToken)).SetPositions(positions);
                await organization.SaveChangesAsync(cancellationToken);
                created++;
            }
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
        if (await organization.NewsArticles.AnyAsync(cancellationToken))
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
                "## Що змінюється\n\nВідтепер накази, розпорядження, службові записки, договори та акти погоджуються **в електронному вигляді**.\n\n- підпис накладається у браузері — ключ і пароль не залишають Ваш пристрій;\n- підтримуються формати CAdES, XAdES, PAdES, ASiC-S та ASiC-E;\n- вхід захищено двофакторною автентифікацією.\n\nПитання щодо доступу — до відділу кадрів або системного адміністратора.",
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
            organization.NewsArticles.Add(article);
        }

        await organization.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the tracked member of an account with its positions, copying the account when the organization does not
    /// know the person yet.
    /// </summary>
    /// <param name="user">The account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The member.</returns>
    private async Task<Member> MemberOfAsync(User user, CancellationToken cancellationToken)
    {
        var member = await organization.Members.Include(static m => m.Positions).FirstOrDefaultAsync(m => m.Id == user.Id, cancellationToken);
        if (member is null)
        {
            member = new Member(MemberProfile.Of(user));
            organization.Members.Add(member);
        }

        return member;
    }
}
