using System.ComponentModel.DataAnnotations;

namespace ODC.Web.Features.Profile;

/// <summary>
/// A code of the authenticator app that confirms a sensitive action (step-up authentication).
/// </summary>
/// <param name="Code">The current authenticator code.</param>
public sealed record StepUpRequest([Required][MaxLength(16)] string Code);
