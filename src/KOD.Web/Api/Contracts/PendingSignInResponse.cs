namespace KOD.Web.Api.Contracts;

/// <summary>
/// A sign-in that passed the password step and waits for the second factor.
/// </summary>
/// <param name="MustChangePassword">Whether the temporary password must be replaced before enrolling an authenticator.</param>
/// <param name="TwoFactorEnabled">Whether an authenticator is enrolled (otherwise it is enrolled now).</param>
public sealed record PendingSignInResponse(bool MustChangePassword, bool TwoFactorEnabled);
