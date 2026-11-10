namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The organization's database was needed but the request has no organization (the person works in none and chose
/// none). Answered as 409 problem details with the code <c>no_organization</c>.
/// </summary>
public sealed class NoOrganizationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoOrganizationException"/> class.
    /// </summary>
    public NoOrganizationException()
        : base("The request has no organization.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOrganizationException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public NoOrganizationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOrganizationException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public NoOrganizationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
