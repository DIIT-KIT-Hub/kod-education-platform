using System.Collections.Frozen;
using System.Security.Claims;
using KOD.Application.Abstractions;
using KOD.Infrastructure.Identity;

namespace KOD.Web.Security;

/// <summary>
/// Resolves the current user from the HTTP request or, inside interactive components, from the circuit.
/// </summary>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="circuitUser">The circuit user accessor.</param>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor, CircuitUserAccessor circuitUser) : ICurrentUser, ISessionContext
{
    /// <summary>
    /// The cached permission set.
    /// </summary>
    private FrozenSet<string>? _permissions;

    /// <summary>
    /// The principal the cached permission set was built from (a circuit may receive a refreshed principal).
    /// </summary>
    private ClaimsPrincipal? _permissionsSource;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public Guid? Id => IsAuthenticated ? Principal!.UserId : null;

    /// <inheritdoc />
    public string? DisplayName => IsAuthenticated ? Principal!.FullName : null;

    /// <inheritdoc />
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public IReadOnlySet<string> Permissions
    {
        get
        {
            var principal = Principal;
            if (_permissions is null || !ReferenceEquals(principal, _permissionsSource))
            {
                _permissionsSource = principal;
                _permissions = principal?.Identity?.IsAuthenticated == true
                    ? principal.FindAll(AppClaimTypes.Permission).Select(static c => c.Value).ToFrozenSet(StringComparer.Ordinal)
                    : FrozenSet<string>.Empty;
            }

            return _permissions;
        }
    }

    /// <inheritdoc />
    public Guid? CurrentSessionId => Principal?.SessionId;

    /// <summary>
    /// Gets the principal of the request or circuit.
    /// </summary>
    private ClaimsPrincipal? Principal => circuitUser.User ?? httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool Has(string permission) => Permissions.Contains(permission);
}
