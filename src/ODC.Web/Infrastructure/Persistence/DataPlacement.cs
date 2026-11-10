using System.Collections.Frozen;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Features.Auth.Activation;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Identity.Tokens;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// Which database every entity lives in: the master database (<see cref="MasterDbContext"/>: accounts, sign-in, the
/// catalogs every organization shares, requests to the platform support) or the database of an organization
/// (<see cref="OrganizationDbContext"/>: its structure, people, documents, news, chats, notifications). Each context
/// applies only the configurations of its own entities; the architecture tests check that every entity is placed.
/// </summary>
/// <remarks>
/// The audit trail is in both: what an organization does is audited in its own database, the sign-in and the platform in
/// the master database. Every organization has a database of its own (the registry: <see cref="Organization"/>).
/// </remarks>
public static class DataPlacement
{
    /// <summary>
    /// Gets the entities of the master database.
    /// </summary>
    public static FrozenSet<Type> Master { get; } = FrozenSet.Create(
        typeof(User),
        typeof(IdentityUserClaim<Guid>),
        typeof(IdentityUserLogin<Guid>),
        typeof(IdentityUserToken<Guid>),
        typeof(Role),
        typeof(RolePermission),
        typeof(UnitType),
        typeof(MediaFile),
        typeof(Ticket),
        typeof(TicketMessage),
        typeof(TicketAttachment),
        typeof(AuditEntry),
        typeof(RefreshTokenFamily),
        typeof(ActivationChallenge),
        typeof(DataProtectionKey),
        typeof(Organization),
        typeof(Membership),
        typeof(Participation),
        typeof(GeneralArticle));

    /// <summary>
    /// Gets the entities of an organization's database.
    /// </summary>
    public static FrozenSet<Type> Organization { get; } = FrozenSet.Create(
        typeof(Member),
        typeof(StaffPosition),
        typeof(UserPermissionOverride),
        typeof(OrganizationUnit),
        typeof(Document),
        typeof(DocumentSigner),
        typeof(DocumentSignature),
        typeof(NewsArticle),
        typeof(NewsComment),
        typeof(NewsReaction),
        typeof(NewsView),
        typeof(Chat),
        typeof(ChatMember),
        typeof(ChatMessage),
        typeof(ChatAttachment),
        typeof(ChatReaction),
        typeof(HiddenChatMessage),
        typeof(Notification),
        typeof(AuditEntry));

    /// <summary>
    /// Whether a configuration class configures only entities of a database (a class may configure several entities).
    /// </summary>
    /// <param name="configuration">The configuration class.</param>
    /// <param name="entities">The entities of the database.</param>
    /// <returns>Whether to apply it.</returns>
    public static bool Configures(Type configuration, IReadOnlySet<Type> entities)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(entities);
        var configured = ConfiguredEntities(configuration).ToList();
        return configured.Count > 0 && configured.TrueForAll(entities.Contains);
    }

    /// <summary>
    /// The entities a configuration class configures.
    /// </summary>
    /// <param name="configuration">The configuration class.</param>
    /// <returns>The entity types.</returns>
    public static IEnumerable<Type> ConfiguredEntities(Type configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetInterfaces()
            .Where(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
            .Select(static i => i.GetGenericArguments()[0]);
    }
}
