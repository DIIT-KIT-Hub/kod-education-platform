namespace KOD.Application.Common.Results;

/// <summary>
/// Category of an application error; maps to an HTTP status code in the presentation layer.
/// </summary>
public enum ErrorType
{
    /// <summary>The input is invalid or violates a business rule (400).</summary>
    Validation = 0,

    /// <summary>The resource does not exist or is not visible to the user (404).</summary>
    NotFound = 1,

    /// <summary>The user is not allowed to perform the operation (403).</summary>
    Forbidden = 2,

    /// <summary>The operation conflicts with the current state (409).</summary>
    Conflict = 3,

    /// <summary>The user is not authenticated (401).</summary>
    Unauthorized = 4,
}
