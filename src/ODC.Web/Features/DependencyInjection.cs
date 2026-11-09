using ODC.Web.Features.Admin.Users;
using ODC.Web.Features.Auth;
using ODC.Web.Features.Chats;
using ODC.Web.Features.Documents;
using ODC.Web.Features.News;
using ODC.Web.Features.News.Discussion;
using ODC.Web.Features.Notifications;
using ODC.Web.Features.Profile;
using ODC.Web.Features.Tickets;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Media;

namespace ODC.Web.Features;

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
            services.AddMessaging(typeof(DependencyInjection).Assembly);
            services.AddScoped<IPermissionResolver, PermissionResolver>();
            services.AddScoped<IAccessScope, AccessScope>();
            services.AddScoped<Catalog>();
            services.AddScoped<PositionDirectory>();

            services.AddScoped<DocumentDesk>();
            services.AddScoped<NewsDesk>();
            services.AddScoped<DiscussionAccess>();
            services.AddScoped<AttachmentStore>();
            services.AddScoped<INotifier, Notifier>();
            services.AddScoped<NotificationInbox>();
            services.AddScoped<ChatRoom>();
            services.AddScoped<TicketDesk>();
            services.AddScoped<MediaLibrary>();
            services.AddScoped<UserDirectory>();
            services.AddScoped<ProfileAccount>();
            services.AddScoped<SignInFlow>();

            return services;
        }
    }
}
