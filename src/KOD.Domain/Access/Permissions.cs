using System.Collections.Frozen;

namespace KOD.Domain.Access;

/// <summary>
/// Catalog of every permission known to the system.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Gets all permission definitions in display order.
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(Documents.View, AppModule.Documents, "Мої документи", "Перегляд документів, автором або підписантом яких є користувач."),
        new(Documents.ViewUnit, AppModule.Documents, "Документи підрозділу", "Перегляд документів свого підрозділу та всіх вкладених підрозділів."),
        new(Documents.ViewOrganization, AppModule.Documents, "Документи організації", "Перегляд документів усієї організації, у якій користувач обіймає посаду, з усіма її підрозділами."),
        new(Documents.ViewAll, AppModule.Documents, "Усі документи", "Перегляд документів усіх організацій у системі."),
        new(Documents.Create, AppModule.Documents, "Створення документів", "Завантаження документів і формування маршруту підписання."),
        new(Documents.Sign, AppModule.Documents, "Підписання документів", "Накладання кваліфікованого електронного підпису на документи."),
        new(Documents.Manage, AppModule.Documents, "Керування документами", "Архівування та видалення будь-яких доступних документів."),
        new(Signing.Use, AppModule.Signing, "Підписання та перевірка файлів", "Інструменти підписання та перевірки довільних файлів у браузері."),
        new(News.View, AppModule.News, "Перегляд новин", "Перегляд новин та оголошень."),
        new(News.Manage, AppModule.News, "Керування новинами", "Створення, редагування та публікація новин."),
        new(Structure.View, AppModule.Structure, "Перегляд структури", "Перегляд організацій, їхніх підрозділів і працівників."),
        new(Structure.Manage, AppModule.Structure, "Керування структурою", "Створення та редагування організацій, підрозділів і типів підрозділів."),
        new(Users.View, AppModule.Users, "Перегляд користувачів", "Перегляд облікових записів користувачів."),
        new(Users.Manage, AppModule.Users, "Керування користувачами", "Створення, редагування та блокування облікових записів, призначення посад."),
        new(Users.Import, AppModule.Users, "Імпорт користувачів", "Масове створення та оновлення облікових записів з файлів XLSX/CSV."),
        new(Users.Export, AppModule.Users, "Експорт користувачів", "Вивантаження облікових записів у файли XLSX/CSV."),
        new(Users.Security, AppModule.Users, "Безпека облікових записів", "Скидання паролів, автентифікаторів і завершення сесій користувачів."),
        new(Roles.Manage, AppModule.Administration, "Керування ролями", "Створення ролей та налаштування їхніх дозволів."),
        new(Administration.AllOrganizations, AppModule.Administration, "Адміністрування всіх організацій", "Дозволи керування користувачами, структурою та новинами діють в усіх організаціях кабінету, а не лише в організаціях власних посад; створення організацій верхнього рівня та керування каталогом типів підрозділів."),
        new(Audit.View, AppModule.Audit, "Журнал аудиту", "Перегляд журналу дій користувачів і подій безпеки."),
    ];

    /// <summary>
    /// Gets the set of all permission codes for fast validation.
    /// </summary>
    public static FrozenSet<string> Codes { get; } = All.Select(static p => p.Code).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Gets the lookup of definitions by code.
    /// </summary>
    public static FrozenDictionary<string, PermissionDefinition> ByCode { get; } =
        All.ToFrozenDictionary(static p => p.Code, StringComparer.Ordinal);

    /// <summary>
    /// Document management permissions.
    /// </summary>
    public static class Documents
    {
        /// <summary>View own documents (authored or assigned for signing).</summary>
        public const string View = "documents.view";

        /// <summary>View documents of own units and their descendants.</summary>
        public const string ViewUnit = "documents.view.unit";

        /// <summary>View documents of the organizations of own positions, including all their units.</summary>
        public const string ViewOrganization = "documents.view.organization";

        /// <summary>View documents of every organization in the system.</summary>
        public const string ViewAll = "documents.view.all";

        /// <summary>Create documents and signing routes.</summary>
        public const string Create = "documents.create";

        /// <summary>Sign documents with a qualified electronic signature.</summary>
        public const string Sign = "documents.sign";

        /// <summary>Archive and delete any accessible document.</summary>
        public const string Manage = "documents.manage";
    }

    /// <summary>
    /// Stand-alone signing tool permissions.
    /// </summary>
    public static class Signing
    {
        /// <summary>Use the sign and verify tools.</summary>
        public const string Use = "signing.use";
    }

    /// <summary>
    /// News permissions.
    /// </summary>
    public static class News
    {
        /// <summary>Read news.</summary>
        public const string View = "news.view";

        /// <summary>Create, edit and publish news.</summary>
        public const string Manage = "news.manage";
    }

    /// <summary>
    /// Organizational structure permissions.
    /// </summary>
    public static class Structure
    {
        /// <summary>View the structure and unit staff.</summary>
        public const string View = "structure.view";

        /// <summary>Create and edit organizations, units and unit types.</summary>
        public const string Manage = "structure.manage";
    }

    /// <summary>
    /// User administration permissions.
    /// </summary>
    public static class Users
    {
        /// <summary>View user accounts.</summary>
        public const string View = "users.view";

        /// <summary>Create, edit, block accounts and assign positions.</summary>
        public const string Manage = "users.manage";

        /// <summary>Import accounts from spreadsheets.</summary>
        public const string Import = "users.import";

        /// <summary>Export accounts to spreadsheets.</summary>
        public const string Export = "users.export";

        /// <summary>Reset passwords, authenticators and sessions of other users.</summary>
        public const string Security = "users.security";
    }

    /// <summary>
    /// Role administration permissions.
    /// </summary>
    public static class Roles
    {
        /// <summary>Create roles and edit their permissions.</summary>
        public const string Manage = "roles.manage";
    }

    /// <summary>
    /// Cabinet-wide administration permissions.
    /// </summary>
    public static class Administration
    {
        /// <summary>
        /// Apply the administrative permissions (<see cref="Users"/>, <see cref="Structure.Manage"/>, <see cref="News.Manage"/>) in every organization
        /// instead of only the organizations of own positions; create top-level organizations and edit the unit type catalog.
        /// </summary>
        public const string AllOrganizations = "administration.all";
    }

    /// <summary>
    /// Audit permissions.
    /// </summary>
    public static class Audit
    {
        /// <summary>Read the audit trail.</summary>
        public const string View = "audit.view";
    }
}
