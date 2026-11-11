namespace ODC.Web.Infrastructure.Files;

/// <summary>
/// A secret of the platform in the master database, encrypted with Data Protection (table <c>platform_secrets</c>): the
/// key of the platform's files (<see cref="FileKeysName"/>).
/// </summary>
public sealed class PlatformSecret
{
    /// <summary>
    /// The name of the key of the platform's files.
    /// </summary>
    public const string FileKeysName = "files";

    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 64;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformSecret"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The encrypted value.</param>
    /// <param name="createdAt">When it was created.</param>
    public PlatformSecret(string name, string value, DateTimeOffset createdAt)
    {
        Name = name;
        Value = value;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the encrypted value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Gets when it was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
