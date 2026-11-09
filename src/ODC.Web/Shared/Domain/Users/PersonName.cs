namespace ODC.Web.Shared.Domain.Users;

/// <summary>
/// The display forms of a person's name, shared by the account (<see cref="User"/>) and its copy in an organization
/// (<see cref="Member"/>).
/// </summary>
public static class PersonName
{
    /// <summary>
    /// The full name: «Прізвище Ім'я По батькові».
    /// </summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The middle name.</param>
    /// <returns>The full name.</returns>
    public static string Full(string lastName, string firstName, string? middleName)
        => middleName is null ? $"{lastName} {firstName}" : $"{lastName} {firstName} {middleName}";

    /// <summary>
    /// The short name: «Прізвище І. П.».
    /// </summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The middle name.</param>
    /// <returns>The short name.</returns>
    public static string Short(string lastName, string firstName, string? middleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstName);
        return string.IsNullOrEmpty(middleName) ? $"{lastName} {firstName[0]}." : $"{lastName} {firstName[0]}. {middleName[0]}.";
    }
}
