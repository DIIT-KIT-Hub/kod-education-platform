using System.Security.Claims;

namespace KOD.Web.Security;

/// <summary>
/// Holds the principal of the current interactive Blazor circuit (where <see cref="HttpContext"/> is not available).
/// </summary>
public sealed class CircuitUserAccessor
{
    /// <summary>
    /// Gets or sets the principal of the circuit.
    /// </summary>
    public ClaimsPrincipal? User { get; set; }
}
