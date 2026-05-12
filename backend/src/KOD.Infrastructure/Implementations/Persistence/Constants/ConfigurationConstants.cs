namespace KOD.Infrastructure.Implementations.Persistence.Constants;

/// <summary>
/// Contains predefined identifiers used for application configuration and seeding.
/// </summary>
public static class ConfigurationConstants
{
    #region Public fields

    /// <summary>
    /// Gets the unique identifier of the root system user.
    /// </summary>
    public static Guid RootUserId => Guid.Parse("46b56073-6b0f-4238-ae89-a81073a4774e");

    /// <summary>
    /// Gets the unique identifier of the administrator role.
    /// </summary>
    public static Guid AdminRoleId => Guid.Parse("a953e99c-2320-4616-b741-b770f2d6bd17");

    /// <summary>
    /// Gets the unique identifier of the default user role.
    /// </summary>
    public static Guid UserRoleId => Guid.Parse("9597b2ae-8059-4b51-beff-c0b295e8a5fa");

    #endregion
}
