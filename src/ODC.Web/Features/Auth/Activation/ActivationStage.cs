namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// Where an activation stands.
/// </summary>
public enum ActivationStage
{
    /// <summary>
    /// The code was requested and waits to be entered.
    /// </summary>
    Code = 0,

    /// <summary>
    /// The code was entered correctly; the owner chooses the password.
    /// </summary>
    Password = 1,

    /// <summary>
    /// The code expired or too many wrong codes were entered; a new one must be requested.
    /// </summary>
    Expired = 2,
}
