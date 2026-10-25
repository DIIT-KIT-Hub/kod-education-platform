using P = KOD.Domain.Access.Permissions;

namespace KOD.Domain.Access;

/// <summary>
/// Predefined roles that model typical positions of a Ukrainian higher education institution.
/// Their permissions can be adjusted by an administrator (except <see cref="Administrator"/>, which always has every permission).
/// </summary>
public static class SystemRoles
{
    /// <summary>System administrator — full access.</summary>
    public const string Administrator = "system-admin";

    /// <summary>Rector.</summary>
    public const string Rector = "rector";

    /// <summary>Vice-rector.</summary>
    public const string ViceRector = "vice-rector";

    /// <summary>Director of an educational and scientific institute.</summary>
    public const string InstituteDirector = "institute-director";

    /// <summary>Dean of a faculty.</summary>
    public const string Dean = "dean";

    /// <summary>Deputy dean.</summary>
    public const string DeputyDean = "deputy-dean";

    /// <summary>Head of a department (кафедра).</summary>
    public const string HeadOfDepartment = "head-of-department";

    /// <summary>Academic staff member: professor, associate professor, senior lecturer, assistant.</summary>
    public const string Lecturer = "lecturer";

    /// <summary>Methodologist / dean's office specialist.</summary>
    public const string Methodologist = "methodologist";

    /// <summary>Head of an administrative division.</summary>
    public const string HeadOfDivision = "head-of-division";

    /// <summary>Records clerk of the general (registry) office.</summary>
    public const string RecordsClerk = "records-clerk";

    /// <summary>Human resources specialist.</summary>
    public const string HrSpecialist = "hr-specialist";

    /// <summary>Press office / communications specialist.</summary>
    public const string PressOfficer = "press-officer";

    /// <summary>General employee without academic duties.</summary>
    public const string Employee = "employee";

    /// <summary>Student (higher education seeker).</summary>
    public const string Student = "student";

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
        new(Administrator, "Системний адміністратор", "Повний доступ до всіх модулів і налаштувань системи.", [.. P.Codes]),
        new(Rector, "Ректор", "Керівник закладу: усі документи закладу, підписання, структура.", [.. BasePermissions, P.Documents.ViewAll, P.Documents.Manage, P.Users.View, P.Audit.View]),
        new(ViceRector, "Проректор", "Заступник керівника закладу за напрямом діяльності.", [.. BasePermissions, P.Documents.ViewAll, P.Users.View]),
        new(InstituteDirector, "Директор інституту", "Керівник навчально-наукового інституту.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View]),
        new(Dean, "Декан", "Керівник факультету.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View]),
        new(DeputyDean, "Заступник декана", "Заступник керівника факультету.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View]),
        new(HeadOfDepartment, "Завідувач кафедри", "Керівник кафедри.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View]),
        new(Lecturer, "Науково-педагогічний працівник", "Професор, доцент, старший викладач, асистент.", BasePermissions),
        new(Methodologist, "Методист", "Фахівець деканату чи навчального відділу.", [.. BasePermissions, P.Documents.ViewUnit]),
        new(HeadOfDivision, "Керівник відділу", "Керівник адміністративного чи сервісного підрозділу.", [.. BasePermissions, P.Documents.ViewUnit, P.Users.View]),
        new(RecordsClerk, "Діловод", "Працівник загального відділу (канцелярії): реєстрація та архівування документів.", [.. BasePermissions, P.Documents.ViewAll, P.Documents.Manage]),
        new(HrSpecialist, "Фахівець відділу кадрів", "Ведення облікових записів працівників, імпорт та експорт.", [.. BasePermissions, P.Users.View, P.Users.Manage, P.Users.Import, P.Users.Export]),
        new(PressOfficer, "Прес-служба", "Публікація новин та оголошень.", [.. BasePermissions, P.News.Manage]),
        new(Employee, "Працівник", "Працівник без особливих повноважень.", BasePermissions),
        new(Student, "Здобувач освіти", "Студент, аспірант: власні документи (заяви) та новини.", [P.Documents.View, P.Documents.Create, P.Documents.Sign, P.Signing.Use, P.News.View]),
    ];
}
