namespace KOD.Domain.Common;

/// <summary>
/// Base type for every domain entity identified by a time-ordered <see cref="Guid"/>.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier of the entity (UUID v7, index friendly).
    /// </summary>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
