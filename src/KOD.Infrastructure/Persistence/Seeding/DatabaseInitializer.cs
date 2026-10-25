using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Domain.Organization;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Persistence.Seeding;

/// <summary>
/// Applies migrations and creates the mandatory initial data: predefined roles, the root unit and the first administrator.
/// Optionally creates a demo structure, demo staff and news.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="identity">The identity service.</param>
/// <param name="options">The seed options.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class DatabaseInitializer(
    ApplicationDbContext db,
    UserManager<User> userManager,
    IIdentityService identity,
    IOptions<SeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
    /// <summary>
    /// Demo units: code, name, short name, type, parent code.
    /// </summary>
    private static readonly (string Code, string Name, string? Short, OrganizationUnitType Type, string? Parent)[] DemoUnits =
    [
        ("RECT", "Ректорат", null, OrganizationUnitType.Division, null),
        ("REG", "Загальний відділ (канцелярія)", "ЗВ", OrganizationUnitType.Division, null),
        ("HR", "Відділ кадрів", "ВК", OrganizationUnitType.Division, null),
        ("EDU", "Навчальний відділ", "НВ", OrganizationUnitType.Division, null),
        ("ACC", "Бухгалтерія", null, OrganizationUnitType.Division, null),
        ("PRESS", "Прес-служба", null, OrganizationUnitType.Division, null),
        ("IIT", "Навчально-науковий інститут інформаційних технологій", "ННІ ІТ", OrganizationUnitType.Institute, null),
        ("SE", "Кафедра програмної інженерії", "ПІ", OrganizationUnitType.Department, "IIT"),
        ("CS", "Кафедра комп'ютерних наук", "КН", OrganizationUnitType.Department, "IIT"),
        ("CYB", "Кафедра кібербезпеки", "КБ", OrganizationUnitType.Department, "IIT"),
        ("ITLAB", "Лабораторія інформаційних систем", null, OrganizationUnitType.Laboratory, "IIT"),
        ("FEM", "Факультет економіки та управління", "ФЕУ", OrganizationUnitType.Faculty, null),
        ("ECON", "Кафедра економіки", null, OrganizationUnitType.Department, "FEM"),
        ("MGMT", "Кафедра менеджменту", null, OrganizationUnitType.Department, "FEM"),
        ("FIN", "Кафедра фінансів, обліку та оподаткування", null, OrganizationUnitType.Department, "FEM"),
        ("FHS", "Факультет гуманітарних наук", "ФГН", OrganizationUnitType.Faculty, null),
        ("PHIL", "Кафедра української філології", null, OrganizationUnitType.Department, "FHS"),
        ("HIST", "Кафедра історії", null, OrganizationUnitType.Department, "FHS"),
        ("LANG", "Кафедра іноземних мов", null, OrganizationUnitType.Department, "FHS"),
    ];

    /// <summary>
    /// Demo staff: e-mail, last name, first name, patronymic, unit code, role code, title.
    /// </summary>
    private static readonly (string Email, string Last, string First, string Middle, string Unit, string Role, string Title)[] DemoStaff =
    [
        ("rector@kod.local", "Шевченко", "Олександр", "Іванович", "RECT", SystemRoles.Rector, "Ректор"),
        ("vice.rector@kod.local", "Мельник", "Наталія", "Петрівна", "RECT", SystemRoles.ViceRector, "Проректор з науково-педагогічної роботи"),
        ("director.iit@kod.local", "Бондаренко", "Андрій", "Сергійович", "IIT", SystemRoles.InstituteDirector, "Директор ННІ ІТ"),
        ("dean.fem@kod.local", "Ткаченко", "Ірина", "Володимирівна", "FEM", SystemRoles.Dean, "Декан ФЕУ"),
        ("head.se@kod.local", "Кравченко", "Дмитро", "Олегович", "SE", SystemRoles.HeadOfDepartment, "Завідувач кафедри програмної інженерії"),
        ("lecturer.se@kod.local", "Коваленко", "Марія", "Андріївна", "SE", SystemRoles.Lecturer, "Доцент"),
        ("methodologist@kod.local", "Олійник", "Світлана", "Миколаївна", "IIT", SystemRoles.Methodologist, "Методист"),
        ("clerk@kod.local", "Шевчук", "Тетяна", "Іванівна", "REG", SystemRoles.RecordsClerk, "Діловод"),
        ("hr@kod.local", "Поліщук", "Оксана", "Василівна", "HR", SystemRoles.HrSpecialist, "Інспектор з кадрів"),
        ("press@kod.local", "Лисенко", "Юлія", "Олександрівна", "PRESS", SystemRoles.PressOfficer, "Керівник прес-служби"),
        ("student@kod.local", "Гончаренко", "Максим", "Ігорович", "SE", SystemRoles.Student, "Студент 3 курсу"),
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
        var root = await SeedRootAsync(cancellationToken);
        await SeedAdministratorAsync(root, roles[SystemRoles.Administrator], cancellationToken);

        if (options.Value.DemoData)
        {
            var units = await SeedDemoUnitsAsync(root, cancellationToken);
            await SeedDemoStaffAsync(units, roles, cancellationToken);
            await SeedDemoNewsAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Logs a generated administrator password.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="email">The administrator e-mail.</param>
    /// <param name="password">The temporary password.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Created the first administrator {Email} with the temporary password {Password}. Change it at the first sign-in.")]
    private static partial void LogAdministratorCreated(ILogger logger, string email, string password);

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
                role = new Role(definition.Code, definition.Name, definition.Description, isSystem: true);
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
    /// Creates the root unit (the institution) when missing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The root unit.</returns>
    private async Task<OrganizationUnit> SeedRootAsync(CancellationToken cancellationToken)
    {
        var root = await db.OrganizationUnits.FirstOrDefaultAsync(static u => u.ParentId == null && u.Type == OrganizationUnitType.University, cancellationToken);
        if (root is not null)
        {
            return root;
        }

        var settings = options.Value;
        root = new OrganizationUnit(settings.UniversityCode, settings.UniversityName, OrganizationUnitType.University, null);
        root.Update(settings.UniversityCode, settings.UniversityName, settings.UniversityShortName, OrganizationUnitType.University, 0);
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
                LogAdministratorCreated(logger, settings.AdminEmail, password);
            }
        }

        admin.SetPositions([(root.Id, adminRole.Id, "Системний адміністратор", true)]);
        await userManager.UpdateAsync(admin);
    }

    /// <summary>
    /// Creates the demo structure.
    /// </summary>
    /// <param name="root">The root unit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units by code.</returns>
    private async Task<Dictionary<string, OrganizationUnit>> SeedDemoUnitsAsync(OrganizationUnit root, CancellationToken cancellationToken)
    {
        var units = await db.OrganizationUnits.ToDictionaryAsync(static u => u.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var order = 0;
        foreach (var (code, name, shortName, type, parent) in DemoUnits)
        {
            if (units.ContainsKey(code))
            {
                continue;
            }

            var parentId = parent is null ? root.Id : units[parent].Id;
            var unit = new OrganizationUnit(code, name, type, parentId);
            unit.Update(code, name, shortName, type, ++order);
            db.OrganizationUnits.Add(unit);
            units[code] = unit;
        }

        await db.SaveChangesAsync(cancellationToken);
        return units;
    }

    /// <summary>
    /// Creates demo staff accounts with a known password (they still enroll an authenticator at the first sign-in).
    /// </summary>
    /// <param name="units">The units by code.</param>
    /// <param name="roles">The roles by code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the accounts exist.</returns>
    private async Task SeedDemoStaffAsync(Dictionary<string, OrganizationUnit> units, Dictionary<string, Role> roles, CancellationToken cancellationToken)
    {
        var password = options.Value.DemoPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var created = 0;
        foreach (var (email, last, first, middle, unit, role, title) in DemoStaff)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new User(email, last, first, middle);
            user.RequirePasswordChange(false);
            user.SetPositions([(units[unit].Id, roles[role].Id, title, true)]);
            if (role == SystemRoles.HeadOfDepartment)
            {
                user.SetPositions([(units[unit].Id, roles[role].Id, title, true), (units[unit].Id, roles[SystemRoles.Lecturer].Id, "Професор", false)]);
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
    /// Creates demo news.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the news exist.</returns>
    private async Task SeedDemoNewsAsync(CancellationToken cancellationToken)
    {
        if (await db.NewsArticles.AnyAsync(cancellationToken))
        {
            return;
        }

        var author = await db.Users.OrderBy(static u => u.CreatedAt).FirstAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        (string Slug, string Title, string Summary, string Body, NewsCategory Category, bool Pinned, int DaysAgo)[] items =
        [
            (
                "zapusk-kabinetu-osvitnikh-danykh",
                "Запущено Кабінет освітніх даних",
                "Університет переходить на електронний документообіг із кваліфікованим електронним підписом.",
                "## Що змінюється\n\nВідтепер накази, розпорядження, службові записки та заяви погоджуються **в електронному вигляді**.\n\n- підпис накладається у браузері — ключ і пароль не залишають ваш пристрій;\n- підтримуються формати CAdES, XAdES, PAdES, ASiC-S та ASiC-E;\n- вхід захищено двофакторною автентифікацією.\n\nПитання щодо доступу — до відділу кадрів або системного адміністратора.",
                NewsCategory.Announcement,
                true,
                1),
            (
                "hrafik-zymovoi-sesii",
                "Затверджено графік зимової екзаменаційної сесії",
                "Деканати оприлюднили розклад іспитів і консультацій для всіх курсів.",
                "Розклад доступний у деканатах та на сторінках інститутів і факультетів.\n\n> Зверніть увагу: ліквідація академічної заборгованості — до кінця навчального семестру.",
                NewsCategory.Education,
                false,
                3),
            (
                "naukova-konferentsiia-tsyfrova-transformatsiia",
                "Наукова конференція «Цифрова трансформація освіти»",
                "Запрошуємо науковців і здобувачів освіти взяти участь у щорічній конференції.",
                "Тези доповідей приймаються до кінця місяця. Робочі мови — українська та англійська.\n\nДеталі — у прес-службі університету.",
                NewsCategory.Science,
                false,
                6),
        ];

        foreach (var item in items)
        {
            var article = new NewsArticle(item.Slug, item.Title, item.Summary, item.Body, item.Category, author.Id);
            article.Update(item.Title, item.Summary, item.Body, item.Category, item.Pinned);
            article.Publish(now.AddDays(-item.DaysAgo));
            db.NewsArticles.Add(article);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
