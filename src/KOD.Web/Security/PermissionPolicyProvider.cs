using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KOD.Web.Security;

/// <summary>
/// Builds policies on the fly for names produced by <see cref="RequirePermissionAttribute"/>
/// (<c>perm:code1|code2</c>), so permissions need no explicit policy registration.
/// </summary>
/// <param name="options">The authorization options.</param>
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    /// <summary>
    /// The prefix of permission policy names.
    /// </summary>
    public const string Prefix = "perm:";

    /// <inheritdoc />
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var permissions = policyName[Prefix.Length..].Split('|', StringSplitOptions.RemoveEmptyEntries);
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions))
            .Build();
    }
}
