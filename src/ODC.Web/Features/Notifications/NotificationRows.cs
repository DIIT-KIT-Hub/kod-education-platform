using System.Linq.Expressions;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Notifications;

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
        n.ReadAt,
        Guid.Empty,
        string.Empty);

    /// <summary>
    /// Names the organization a notification comes from.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="organization">Its organization.</param>
    /// <returns>The notification with its organization.</returns>
    public static NotificationDto In(NotificationDto notification, Organization organization)
        => notification with { OrganizationId = organization.Id, OrganizationCode = organization.Code };
}
