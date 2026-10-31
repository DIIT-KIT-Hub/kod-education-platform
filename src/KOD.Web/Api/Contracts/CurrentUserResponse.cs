namespace KOD.Web.Api.Contracts;

/// <summary>
/// The signed-in user as the access token describes them.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Email">The e-mail address (login).</param>
/// <param name="FullName">The full name.</param>
/// <param name="ShortName">The name with initials.</param>
/// <param name="PhotoId">The profile photo; the cabinet cannot be used until there is one.</param>
/// <param name="MustChangePassword">Whether a temporary password must be replaced first.</param>
/// <param name="Permissions">The effective permission codes.</param>
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string ShortName,
    Guid? PhotoId,
    bool MustChangePassword,
    IReadOnlyList<string> Permissions);
