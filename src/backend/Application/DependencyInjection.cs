using Application.Features.AccessManagement;
using Application.Features.Events;
using Application.Features.Sharing;
using Application.Features.Videos;
﻿using Application.Features.Groups;
﻿using Application.Features.Comments;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterApplicationServices()
        {

            services
                .AddScoped<ICommentService, CommentService>()
                .AddScoped<IIdentityClient, IdentityClient>()
                .AddScoped<ISharedLinkService, SharedLinkService>();

            services.AddCommentsFeature();

            services.AddSharingFeature();
            services.AddGroupsFeature();
            services.AddEventsFeature();
            services.AddAccessManagementFeature();
            services.AddVideosFeature();

            // FastEndpoints discovery. The endpoint classes live in this (Application) assembly, so
            // point discovery at it explicitly rather than relying on AppDomain scanning.
            services.AddFastEndpoints(o => o.Assemblies = [typeof(DependencyInjection).Assembly]);

            return services;
        }
    }

    /// <summary>
    /// Maps the FastEndpoints routes. Call after UseAuthentication()/UseAuthorization().
    /// Endpoints opt into auth via Policies(...) (e.g. the ReadScope/WriteConvert policies registered
    /// in the API host); anonymous endpoints call AllowAnonymous().
    /// </summary>
    public static IApplicationBuilder UseApplicationEndpoints(this IApplicationBuilder app)
    {
        app.UseFastEndpoints(c =>
        {
            // Our *Routes constants already include the "api/" prefix, so don't let FE add another.
            c.Endpoints.RoutePrefix = null;
        });

        return app;
    }
}
