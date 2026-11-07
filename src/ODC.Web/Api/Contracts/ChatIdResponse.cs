namespace ODC.Web.Api.Contracts;

/// <summary>
/// The chat a request opened or created.
/// </summary>
/// <param name="Id">The chat identifier.</param>
public sealed record ChatIdResponse(Guid Id);
