using System.Linq.Expressions;
using KOD.Application.Common.Models;
using KOD.Domain.Notifications;

namespace KOD.Application.Features.Notifications;

/// <summary>
/// The database projection of notifications.
/// </summary>
internal static class NotificationRows
{
    /// <summary>
    /// Gets the projection of a notification for display (translatable to SQL).
    /// </summary>
    public static Expression<Func<Notification, NotificationDto>> Dto { get; } = static n => new NotificationDto(
        n.Id,
        n.Kind,
        n.ActorId == null
            ? null
            : new UserRef(n.Actor!.Id, n.Actor.LastName + " " + n.Actor.FirstName + (n.Actor.MiddleName != null ? " " + n.Actor.MiddleName : string.Empty), n.Actor.PhotoId),
        n.EntityId,
        n.Subject,
        n.Preview,
        n.Code,
        n.Link,
        n.Count,
        n.CreatedAt,
        n.ReadAt);
}
