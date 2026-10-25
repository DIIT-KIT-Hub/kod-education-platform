namespace KOD.Domain.Common;

/// <summary>
/// Marks an entity whose creation and modification timestamps are maintained automatically by the persistence layer.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the moment the entity was created (UTC).
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the moment the entity was last modified (UTC).
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }
}
