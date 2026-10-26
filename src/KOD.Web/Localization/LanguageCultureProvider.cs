using System.Security.Claims;
using KOD.Domain.Localization;
using KOD.Infrastructure.Identity;
using Microsoft.AspNetCore.Localization;

namespace KOD.Web.Localization;

/// <summary>
/// Chooses the request culture: the language saved in the signed-in user's profile (the <c>kod:lang</c> claim), otherwise
/// the language cookie (set by the language switch, also on the sign-in pages), otherwise Ukrainian.
/// Must run after authentication. Interactive circuits keep the culture of the request that started them, so changing the
/// language always ends with a full page load.
/// </summary>
internal sealed class LanguageCultureProvider : RequestCultureProvider
{
    /// <summary>
    /// The cookie that remembers the language in this browser.
    /// </summary>
    public const string CookieName = "__Host-kod.lang";

    /// <summary>
    /// Stores the language in the browser cookie.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="language">The language code.</param>
    public static void Remember(HttpResponse response, string language)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Append(CookieName, language, new CookieOptions
        {
            Path = "/",
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
        });
    }

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var language = Languages.Normalize(httpContext.User.FindFirstValue(AppClaimTypes.Language))
            ?? Languages.Normalize(httpContext.Request.Cookies[CookieName]);
        if (language is null)
        {
            return NullProviderCultureResult;
        }

        var culture = Languages.CultureOf(language);
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}
