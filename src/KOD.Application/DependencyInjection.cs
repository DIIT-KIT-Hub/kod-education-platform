using KOD.Application.Common.Access;
using KOD.Application.Features.Audit;
using KOD.Application.Features.Dashboard;
using KOD.Application.Features.Documents;
using KOD.Application.Features.Media;
using KOD.Application.Features.News;
using KOD.Application.Features.News.Discussion;
using KOD.Application.Features.Organization;
using KOD.Application.Features.Profile;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Signing;
using KOD.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace KOD.Application;

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
            services.AddSingleton<INewsDiscussionEvents, NewsDiscussionEvents>();
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
