namespace KOD.Application.Common.Email;

/// <summary>
/// The content of an e-mail in the cabinet's layout.
/// </summary>
/// <param name="Subject">The subject.</param>
/// <param name="Badge">The small label above the title (what happened).</param>
/// <param name="Title">The title.</param>
/// <param name="Intro">The paragraph under the title.</param>
/// <param name="Details">The rows of details: label and value.</param>
/// <param name="Quote">A quoted text (the beginning of a message), or <see langword="null"/>.</param>
/// <param name="ButtonText">The label of the button.</param>
/// <param name="ButtonUrl">The address the button opens.</param>
/// <param name="Footer">Why the recipient gets the e-mail.</param>
public sealed record EmailContent(
    string Subject,
    string Badge,
    string Title,
    string Intro,
    IReadOnlyList<KeyValuePair<string, string>> Details,
    string? Quote,
    string ButtonText,
    string ButtonUrl,
    string Footer);
