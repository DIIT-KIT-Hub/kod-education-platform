using P = KOD.Domain.Access.Permissions;

namespace KOD.Domain.Access;

/// <summary>
/// Predefined roles that model positions found in almost any organization, whatever its domain.
/// Their names and permissions can be adjusted by an administrator (except <see cref="Administrator"/>, which always has every permission),
/// and domain-specific roles (e.g. «Декан», «Головний лікар») are added as custom roles.
/// </summary>
public static class SystemRoles
{
    /// <summary>System administrator — full access.</summary>
    public const string Administrator = "system-admin";

    /// <summary>Head of an organization (director, CEO, chairperson, rector…).</summary>
    public const string Head = "head";

    /// <summary>Deputy head of an organization.</summary>
    public const string DeputyHead = "deputy-head";

    /// <summary>Head of a unit (department, division, sector, branch…).</summary>
    public const string HeadOfUnit = "head-of-unit";

    /// <summary>Deputy head of a unit.</summary>
    public const string DeputyHeadOfUnit = "deputy-head-of-unit";

    /// <summary>Records clerk of the general (registry) office.</summary>
    public const string RecordsClerk = "records-clerk";

    /// <summary>Human resources specialist.</summary>
    public const string HrSpecialist = "hr-specialist";

    /// <summary>Communications / press office specialist.</summary>
    public const string Communications = "communications";

    /// <summary>Employee without special powers.</summary>
    public const string Employee = "employee";

    /// <summary>External user outside the staff (contractor, client, trainee, student…).</summary>
    public const string External = "external";

    /// <summary>
    /// Permissions every staff member gets.
    /// </summary>
    private static readonly string[] BasePermissions =
        [P.Documents.View, P.Documents.Create, P.Documents.Sign, P.Signing.Use, P.News.View, P.Structure.View];

    /// <summary>
    /// Gets the definitions of all predefined roles.
    /// </summary>
    public static IReadOnlyList<SystemRoleDefinition> All { get; } =
    [
        new(Administrator, "Системний адміністратор", "Повний доступ до всіх модулів і налаштувань системи.", [.. P.Codes], IsLeadership: false),
        new(Head, "Керівник організації", "Директор, голова, ректор чи інший перший керівник: усі документи організації, підписання, працівники та журнал аудиту.", [.. BasePermissions, P.Documents.ViewOrganization, P.Documents.Manage, P.Users.View, P.Audit.View], IsLeadership: true),
        new(DeputyHead, "Заступник керівника", "Заступник першого керівника організації за напрямом діяльності.", [.. BasePermissions, P.Documents.ViewOrganization, P.Users.View], IsLeadership: true),
        new(HeadOfUnit, "Керівник підрозділу", "Керівник департаменту, управління, відділу, сектору чи філії: документи свого підрозділу та вкладених.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View], IsLeadership: true),
        new(DeputyHeadOfUnit, "Заступник керівника підрозділу", "Заступник керівника підрозділу.", [.. BasePermissions, P.Documents.ViewUnit], IsLeadership: true),
        new(RecordsClerk, "Діловод", "Працівник канцелярії (загального відділу): реєстрація та архівування документів організації.", [.. BasePermissions, P.Documents.ViewOrganization, P.Documents.Manage], IsLeadership: false),
        new(HrSpecialist, "Фахівець з персоналу", "Ведення облікових записів працівників, імпорт та експорт.", [.. BasePermissions, P.Users.View, P.Users.Manage, P.Users.Import, P.Users.Export], IsLeadership: false),
        new(Communications, "Фахівець з комунікацій", "Публікація новин та оголошень.", [.. BasePermissions, P.News.Manage], IsLeadership: false),
        new(Employee, "Працівник", "Працівник без особливих повноважень.", BasePermissions, IsLeadership: false),
        new(External, "Зовнішній користувач", "Особа поза штатом (контрагент, клієнт, слухач, здобувач освіти): власні документи та новини, без доступу до структури.", [P.Documents.View, P.Documents.Create, P.Documents.Sign, P.Signing.Use, P.News.View], IsLeadership: false),
    ];
}
