namespace KOD.Domain.Organization;

/// <summary>
/// Kind of a structural unit of a higher education institution (according to the Law of Ukraine "On Higher Education", art. 33).
/// </summary>
public enum OrganizationUnitType
{
    /// <summary>
    /// The higher education institution itself (university, academy, institute as a legal entity).
    /// </summary>
    University = 0,

    /// <summary>
    /// Educational and scientific institute (навчально-науковий інститут) — groups faculties and/or departments.
    /// </summary>
    Institute = 1,

    /// <summary>
    /// Faculty (факультет) — groups at least three departments.
    /// </summary>
    Faculty = 2,

    /// <summary>
    /// Department (кафедра) — the base academic unit.
    /// </summary>
    Department = 3,

    /// <summary>
    /// Administrative or service division (rectorate, HR, accounting, registry office, etc.).
    /// </summary>
    Division = 4,

    /// <summary>
    /// Research or educational laboratory / center.
    /// </summary>
    Laboratory = 5,

    /// <summary>
    /// Separate structural subdivision (college, professional college, branch).
    /// </summary>
    Branch = 6,
}
