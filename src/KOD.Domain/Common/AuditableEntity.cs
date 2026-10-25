namespace KOD.Domain.Common;

/// <summary>
/// Base type for entities that track creation and modification timestamps.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
