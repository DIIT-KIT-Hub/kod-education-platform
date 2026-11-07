using Microsoft.Extensions.DependencyInjection;
using ODC.Application.Common.Access;
using ODC.Application.Common.Files;
using ODC.Application.Features.Audit;
using ODC.Application.Features.Chats;
using ODC.Application.Features.Dashboard;
using ODC.Application.Features.Documents;
using ODC.Application.Features.Media;
using ODC.Application.Features.News;
using ODC.Application.Features.News.Discussion;
using ODC.Application.Features.Notifications;
using ODC.Application.Features.Organization;
using ODC.Application.Features.Profile;
using ODC.Application.Features.Roles;
using ODC.Application.Features.Signing;
using ODC.Application.Features.Tickets;
using ODC.Application.Features.Users;

namespace ODC.Application;

/// <summary>
/// Registers the application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the use case services and access rules.
        /// </summary>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddApplication()
        {
            services.AddScoped<IPermissionResolver, PermissionResolver>();
            services.AddScoped<IAccessScope, AccessScope>();

            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<INewsDiscussionService, NewsDiscussionService>();
            services.AddScoped<AttachmentStore>();
            services.AddScoped<INotifier, Notifier>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<TicketMail>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<MediaLibrary>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IUnitTypeService, UnitTypeService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ISigningJournal, SigningJournal>();

            return services;
        }
    }
}
