using KOD.Application.Common.Paging;

namespace KOD.Application.Features.Audit;

/// <summary>
/// Paging and filter parameters of the audit trail.
/// </summary>
public sealed record AuditQuery : PageRequest
{
    /// <summary>
    /// Gets the action code prefix filter (e.g. <c>auth.</c>).
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Gets the start of the period (inclusive).
    /// </summary>
    public DateOnly? From { get; init; }

    /// <summary>
    /// Gets the end of the period (inclusive).
    /// </summary>
    public DateOnly? To { get; init; }
}
