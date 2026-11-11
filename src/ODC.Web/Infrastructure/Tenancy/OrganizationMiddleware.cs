using System.Security.Claims;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Chooses the organization of a request after the sign-in is known: the one of the address (<c>/o/{code}</c>) or the
/// person's default one (the earliest membership; for a platform administrator without memberships, the first
/// organization). Decides why the person may act there (<see cref="OrganizationAccess"/>) and replaces the permissions of
/// the request with those of that organization (<see cref="OrganizationPermissions"/>), so the permission policies and
/// <c>ICurrentUser</c> work per organization. An unknown or suspended organization answers 404.
/// </summary>
/// <param name="next">The next middleware.</param>
public sealed class OrganizationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The request item with the <see cref="OrganizationContext"/> of the request (the SignalR connection made by the
    /// request keeps it, see <see cref="OrganizationHubFilter"/>).
    /// </summary>
    public const string ContextItem = "odc.organization";

    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="registry">The registry of organizations.</param>
    /// <param name="organization">The organization of the request.</param>
    /// <param name="memberships">The memberships of people.</param>
    /// <param name="cache">The access cache.</param>
    /// <param name="schema">The schema the organizations' databases must have.</param>
    /// <returns>A task.</returns>
    public async Task InvokeAsync(HttpContext context, OrganizationRegistry registry, OrganizationContext organization, Memberships memberships, AccessCache cache, OrganizationSchema schema)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(memberships);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(schema);
        var cancellationToken = context.RequestAborted;
        context.Items[ContextItem] = organization;

        Organization? chosen = null;
        if (context.Items[OrganizationPathMiddleware.CodeItem] is string code)
        {
            chosen = await registry.FindByCodeAsync(code, cancellationToken);
            if (chosen is not { IsActive: true })
            {
                await Results.Problem(Texts.T("Організацію не знайдено."), statusCode: StatusCodes.Status404NotFound, extensions: new Dictionary<string, object?> { ["code"] = "organization_not_found" })
                    .ExecuteAsync(context);
                return;
            }

            if (!schema.IsCurrent(chosen))
            {
                await Unavailable(context);
                return;
            }
        }

        var user = context.User;
        if (user.UserId is not { } userId)
        {
            if (chosen is not null)
            {
                organization.Enter(chosen, OrganizationAccess.Anonymous);
            }

            await next(context);
            return;
        }

        var token = user.FindAll(AppClaimTypes.Permission).Select(static c => c.Value).ToHashSet(StringComparer.Ordinal);
        var platform = OrganizationPermissions.IsPlatformAdministrator(token);
        var mine = await memberships.OfAsync(userId, cancellationToken);
        if (chosen is null)
        {
            var all = await registry.AllAsync(cancellationToken);
            chosen = mine.Select(id => all.FirstOrDefault(o => o.Id == id)).FirstOrDefault(o => o is { IsActive: true } && schema.IsCurrent(o))
                ?? (platform ? all.FirstOrDefault(o => o.IsActive && schema.IsCurrent(o)) : null);
        }

        HashSet<string> effective;
        if (chosen is null)
        {
            effective = OrganizationPermissions.Effective(OrganizationAccess.Anonymous, token, new HashSet<string>());
        }
        else
        {
            // A platform administrator holds every permission everywhere; where they also work they are a member (their
            // work there is the organization's own), elsewhere they use the platform's powers (audited twice).
            var access = mine.Contains(chosen.Id) ? OrganizationAccess.Member : platform ? OrganizationAccess.Platform : OrganizationAccess.Guest;
            organization.Enter(chosen, access);
            if (access == OrganizationAccess.Platform)
            {
                // What a platform administrator creates there points at their profile copy.
                await context.RequestServices.GetRequiredService<ExternalParticipants>().EnsurePresentAsync(userId, cancellationToken);
            }

            IReadOnlySet<string> here = access == OrganizationAccess.Member && !platform
                ? await cache.GetAsync(
                    $"permissions:{userId}:{chosen.Id}",
                    async () => (IReadOnlySet<string>)(await context.RequestServices.GetRequiredService<IPermissionResolver>().GetEffectivePermissionsAsync(userId, cancellationToken)).ToHashSet(StringComparer.Ordinal))
                : new HashSet<string>();
            effective = OrganizationPermissions.Effective(platform ? OrganizationAccess.Platform : access, token, here);
        }

        context.User = WithPermissions(user, effective);
        await next(context);
    }

    /// <summary>
    /// Answers that the organization is unavailable for now (its database is not up to date).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task.</returns>
    private static Task Unavailable(HttpContext context)
        => Results.Problem(Texts.T("Організація тимчасово недоступна: її базу даних оновлюють."), statusCode: StatusCodes.Status503ServiceUnavailable, extensions: new Dictionary<string, object?> { ["code"] = "organization_unavailable" })
            .ExecuteAsync(context);

    /// <summary>
    /// Replaces the permission claims of the signed-in identity (the other claims and their issuer stay, so the
    /// antiforgery token, bound to the subject and its issuer, keeps working).
    /// </summary>
    /// <param name="user">The principal.</param>
    /// <param name="permissions">The permissions.</param>
    /// <returns>The new principal.</returns>
    private static ClaimsPrincipal WithPermissions(ClaimsPrincipal user, IEnumerable<string> permissions)
    {
        if (user.Identity is not ClaimsIdentity identity)
        {
            return user;
        }

        var issuer = identity.FindFirst(AppClaimTypes.Subject)?.Issuer ?? ClaimsIdentity.DefaultIssuer;
        var replaced = identity.Clone();
        foreach (var claim in replaced.FindAll(AppClaimTypes.Permission).ToList())
        {
            replaced.RemoveClaim(claim);
        }

        replaced.AddClaims(permissions.Order(StringComparer.Ordinal).Select(p => new Claim(AppClaimTypes.Permission, p, ClaimValueTypes.String, issuer, issuer)));
        return new ClaimsPrincipal([replaced, .. user.Identities.Where(i => !ReferenceEquals(i, identity))]);
    }
}
