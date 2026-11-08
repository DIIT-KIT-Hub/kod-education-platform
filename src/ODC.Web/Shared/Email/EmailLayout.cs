using System.Globalization;
using System.Net;
using System.Text;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Shared.Email;

/// <summary>
/// Renders e-mails in the cabinet's monochrome style (white card on light grey, black mark and pill button, Manrope with
/// system fallbacks). Mail programs ignore style sheets, so every style is inline and the layout uses tables.
/// </summary>
public static class EmailLayout
{
    /// <summary>
    /// The font stack: Manrope where it is installed, then the system sans-serif fonts.
    /// </summary>
    private const string Font = "Manrope,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    /// <summary>
    /// Builds the e-mail to a person.
    /// </summary>
    /// <param name="to">The address.</param>
    /// <param name="toName">The name of the recipient.</param>
    /// <param name="content">The content.</param>
    /// <returns>The e-mail with its HTML and plain text bodies.</returns>
    public static EmailMessage Render(string to, string toName, EmailContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new EmailMessage(to, toName, content.Subject, Html(content), Text(content));
    }

    /// <summary>
    /// Runs a composition in a language: the texts and the formats of dates follow the recipient, not the request.
    /// </summary>
    /// <typeparam name="T">The result.</typeparam>
    /// <param name="language">The language of the recipient (<see cref="Languages"/>), or <see langword="null"/> for Ukrainian.</param>
    /// <param name="compose">Composes the e-mail.</param>
    /// <returns>The result of <paramref name="compose"/>.</returns>
    public static T InLanguage<T>(string? language, Func<T> compose)
    {
        ArgumentNullException.ThrowIfNull(compose);
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var target = CultureInfo.GetCultureInfo(Languages.CultureOf(Languages.Normalize(language) ?? Languages.Ukrainian));
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;
        try
        {
            return compose();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    /// <summary>
    /// Renders the HTML body.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The HTML.</returns>
    private static string Html(EmailContent content)
    {
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"""
            <!DOCTYPE html>
            <html lang="{Languages.Current}">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="color-scheme" content="light">
            <title>{E(content.Subject)}</title>
            </head>
            <body style="margin:0;padding:0;background:#f5f5f5;">
            <div style="display:none;max-height:0;overflow:hidden;">{E(content.Intro)}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;font-family:{Font};color:#000000;">
            <tr><td align="center" style="padding:32px 16px;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">
            <tr><td style="padding:0 8px 20px;">
            <table role="presentation" cellpadding="0" cellspacing="0"><tr>
            <td width="40" height="40" align="center" valign="middle" style="width:40px;height:40px;background:#000000;border-radius:12px;color:#ffffff;font-size:20px;font-weight:800;line-height:40px;">{E(Texts.In("brand", "К"))}</td>
            <td style="padding-left:12px;">
            <div style="font-size:18px;font-weight:800;letter-spacing:0.04em;line-height:1.2;">{E(Texts.T("КОД"))}</div>
            <div style="font-size:12px;font-weight:600;color:#555555;line-height:1.4;">{E(Texts.T("Кабінет організаційних документів"))}</div>
            </td>
            </tr></table>
            </td></tr>
            <tr><td style="background:#ffffff;border:1px solid #e2e2e2;border-radius:24px;padding:32px;">
            <span style="display:inline-block;padding:4px 12px;border:1px solid #000000;border-radius:999px;font-size:12px;font-weight:700;">{E(content.Badge)}</span>
            <h1 style="margin:16px 0 8px;font-size:24px;line-height:1.25;font-weight:800;">{E(content.Title)}</h1>
            <p style="margin:0 0 24px;font-size:15px;line-height:1.6;color:#555555;">{E(content.Intro)}</p>
            {CodeBlock(content.Code)}
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
            """);
        foreach (var (label, value) in content.Details)
        {
            html.Append(CultureInfo.InvariantCulture, $"""
                <tr><td width="38%" style="padding:10px 12px 10px 0;border-top:1px solid #ededed;color:#757575;vertical-align:top;">{E(label)}</td><td style="padding:10px 0;border-top:1px solid #ededed;font-weight:600;vertical-align:top;">{E(value)}</td></tr>
                """);
        }

        html.Append("</table>");
        if (!string.IsNullOrWhiteSpace(content.Quote))
        {
            html.Append(CultureInfo.InvariantCulture, $"""
                <div style="margin-top:24px;padding:16px 20px;background:#f5f5f5;border-radius:16px;font-size:14px;line-height:1.6;white-space:pre-line;">{E(content.Quote)}</div>
                """);
        }

        html.Append(CultureInfo.InvariantCulture, $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin-top:28px;"><tr>
            <td style="background:#000000;border-radius:999px;"><a href="{E(content.ButtonUrl)}" style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:700;color:#ffffff;text-decoration:none;">{E(content.ButtonText)}</a></td>
            </tr></table>
            </td></tr>
            <tr><td style="padding:20px 8px 0;font-size:12px;line-height:1.6;color:#757575;">{E(content.Footer)}</td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);
        return html.ToString();
    }

    /// <summary>
    /// Renders the one-time code: large digits in a grey box, spaced so they read and copy easily.
    /// </summary>
    /// <param name="code">The code, or <see langword="null"/>.</param>
    /// <returns>The HTML, or an empty string.</returns>
    private static string CodeBlock(string? code)
        => string.IsNullOrEmpty(code)
            ? string.Empty
            : $"""<div style="margin:0 0 8px;padding:20px;background:#f5f5f5;border-radius:16px;text-align:center;font-size:34px;font-weight:800;letter-spacing:0.3em;font-family:Consolas,Menlo,monospace;">{E(code)}</div>""";

    /// <summary>
    /// Renders the plain text body.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The text.</returns>
    private static string Text(EmailContent content)
    {
        var text = new StringBuilder();
        text.AppendLine(content.Title).AppendLine().AppendLine(content.Intro).AppendLine();
        if (!string.IsNullOrEmpty(content.Code))
        {
            text.AppendLine(content.Code).AppendLine();
        }

        foreach (var (label, value) in content.Details)
        {
            text.Append(label).Append(": ").AppendLine(value);
        }

        if (!string.IsNullOrWhiteSpace(content.Quote))
        {
            text.AppendLine().AppendLine(content.Quote);
        }

        text.AppendLine().Append(content.ButtonText).Append(": ").AppendLine(content.ButtonUrl);
        text.AppendLine().AppendLine("—").Append(content.Footer);
        return text.ToString();
    }

    /// <summary>
    /// Encodes a text for HTML.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The encoded text.</returns>
    private static string E(string value) => WebUtility.HtmlEncode(value);
}
