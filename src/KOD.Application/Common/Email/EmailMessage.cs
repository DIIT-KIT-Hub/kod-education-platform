namespace KOD.Application.Common.Email;

/// <summary>
/// An e-mail to one person (every recipient gets their own copy, in their language, without the others' addresses).
/// </summary>
/// <param name="To">The address.</param>
/// <param name="ToName">The name of the recipient.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Html">The HTML body.</param>
/// <param name="Text">The plain text body (mail programs without HTML).</param>
public sealed record EmailMessage(string To, string ToName, string Subject, string Html, string Text);
