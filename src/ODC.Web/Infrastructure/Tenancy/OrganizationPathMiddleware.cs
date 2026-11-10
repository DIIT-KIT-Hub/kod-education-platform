using System.Text.RegularExpressions;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Takes the organization out of the address: <c>/o/{code}/api/v1/documents</c> is served as <c>/api/v1/documents</c>
/// with the path base <c>/o/{code}</c> and the code kept for <see cref="OrganizationMiddleware"/>. Links built from the
/// path base (file addresses) stay inside the organization. Addresses without the prefix use the person's default
/// organization.
/// </summary>
/// <param name="next">The next middleware.</param>
public sealed partial class OrganizationPathMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The key of the requested code in <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string CodeItem = "odc.organization-code";

    /// <summary>
    /// The first segment of an organization's addresses.
    /// </summary>
    public const string Prefix = "/o";

    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Path.StartsWithSegments(Prefix, out var rest) && rest.Value is { Length: > 1 } value)
        {
            var slash = value.IndexOf('/', 1);
            var code = slash < 0 ? value[1..] : value[1..slash];
            if (Code().IsMatch(code))
            {
                context.Request.PathBase = context.Request.PathBase.Add(new PathString($"{Prefix}/{code}"));
                context.Request.Path = slash < 0 ? PathString.Empty : new PathString(value[slash..]);
                context.Items[CodeItem] = code;
            }
        }

        return next(context);
    }

    /// <summary>
    /// The form of an organization's code.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("^[A-Za-z0-9-]{1,32}$")]
    private static partial Regex Code();
}
