using System.ComponentModel.DataAnnotations;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The one-time code from the activation e-mail.
/// </summary>
/// <param name="Code">The code (six digits; spaces and dashes are ignored).</param>
public sealed record ActivationCodeRequest([Required][MaxLength(32)] string Code);
