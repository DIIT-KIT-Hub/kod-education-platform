namespace ODC.Web.Api.Contracts;

/// <summary>
/// The identifier of a created user.
/// </summary>
/// <param name="Id">The user identifier.</param>
public sealed record UserIdResponse(Guid Id);
