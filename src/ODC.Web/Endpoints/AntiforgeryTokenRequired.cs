namespace ODC.Web.Endpoints;

/// <summary>
/// Endpoint metadata: state-changing requests need the antiforgery token (<see cref="AntiforgeryEndpointFilter"/>).
/// Lets the architecture tests check every endpoint without calling it.
/// </summary>
public sealed class AntiforgeryTokenRequired
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AntiforgeryTokenRequired"/> class.
    /// </summary>
    private AntiforgeryTokenRequired()
    {
    }

    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static AntiforgeryTokenRequired Instance { get; } = new();
}
