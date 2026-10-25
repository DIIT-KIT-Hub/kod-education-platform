using KOD.Domain.Common;

namespace KOD.Domain.Audit;

/// <summary>
/// An immutable record of a security-relevant or business action.
/// </summary>
public sealed class AuditEntry : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntry"/> class.
    /// </summary>
    /// <param name="occurredAt">The moment of the action.</param>
    /// <param name="action">The action code (see <see cref="AuditActions"/>).</param>
    /// <param name="userId">The acting user identifier.</param>
    /// <param name="userName">The acting user's display name at the moment of the action.</param>
    /// <param name="entityType">The affected entity type.</param>
    /// <param name="entityId">The affected entity identifier.</param>
    /// <param name="details">A short human-readable description.</param>
    /// <param name="ipAddress">The client IP address.</param>
    public AuditEntry(
        DateTimeOffset occurredAt,
        string action,
        Guid? userId,
        string? userName,
        string? entityType,
        string? entityId,
        string? details,
        string? ipAddress)
    {
        OccurredAt = occurredAt;
        Action = action;
        UserId = userId;
        UserName = userName;
        EntityType = entityType;
        EntityId = entityId;
        Details = details is { Length: > 2000 } ? details[..2000] : details;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntry"/> class for EF Core materialization.
    /// </summary>
    private AuditEntry()
    {
        Action = string.Empty;
    }

    /// <summary>
    /// Gets the moment of the action.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Gets the action code.
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Gets the acting user identifier.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the acting user's display name.
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// Gets the affected entity type.
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// Gets the affected entity identifier.
    /// </summary>
    public string? EntityId { get; private set; }

    /// <summary>
    /// Gets the human-readable description.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    public string? IpAddress { get; private set; }
}
