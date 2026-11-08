using System.ComponentModel.DataAnnotations;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The first step of activating an account: the e-mail the code is sent to.
/// </summary>
/// <param name="Email">The e-mail address (login).</param>
public sealed record ActivationStartRequest([Required][MaxLength(256)] string Email);
