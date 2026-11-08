namespace ODC.Web.Endpoints;

/// <summary>
/// The body of a 201 answer: the identifier of the created record (its address is in the <c>Location</c> header).
/// </summary>
/// <param name="Id">The identifier.</param>
public sealed record CreatedId(Guid Id);
