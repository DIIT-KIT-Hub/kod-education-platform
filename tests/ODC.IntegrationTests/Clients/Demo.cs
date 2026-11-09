namespace ODC.IntegrationTests.Clients;

/// <summary>
/// The demo accounts (<c>Seed:DemoData</c>) the tests use; each one has one position in one organization. The first
/// organization is «Моя організація» (code <c>ORG</c>), the second the charity fund (code <c>FUND</c>).
/// </summary>
public static class Demo
{
    /// <summary>
    /// The head of the first organization (all its documents, staff and audit).
    /// </summary>
    public const string Director = "director@odc.local";

    /// <summary>
    /// The deputy head of the first organization (all its documents).
    /// </summary>
    public const string Deputy = "deputy@odc.local";

    /// <summary>
    /// An engineer of the first organization (a plain employee).
    /// </summary>
    public const string Engineer = "engineer@odc.local";

    /// <summary>
    /// The records clerk of the first organization (manages its documents).
    /// </summary>
    public const string Clerk = "clerk@odc.local";

    /// <summary>
    /// The HR specialist of the first organization (manages its users).
    /// </summary>
    public const string Hr = "hr@odc.local";

    /// <summary>
    /// The head of the IT department of the first organization.
    /// </summary>
    public const string HeadOfIt = "head.it@odc.local";

    /// <summary>
    /// The head of the finance department of the first organization.
    /// </summary>
    public const string HeadOfFinance = "head.fin@odc.local";

    /// <summary>
    /// The press manager of the first organization (publishes its news).
    /// </summary>
    public const string Press = "press@odc.local";

    /// <summary>
    /// The contractor's representative of the first organization: the sign-in tests lock this account out, so no other
    /// test may use it.
    /// </summary>
    public const string LockedOut = "external@odc.local";

    /// <summary>
    /// The head of the second organization, the charity fund.
    /// </summary>
    public const string FundHead = "fund@odc.local";

    /// <summary>
    /// The HR manager of the second organization.
    /// </summary>
    public const string FundHr = "hr.fund@odc.local";

    /// <summary>
    /// A project coordinator of the second organization (a plain employee).
    /// </summary>
    public const string FundEmployee = "projects.fund@odc.local";
}
