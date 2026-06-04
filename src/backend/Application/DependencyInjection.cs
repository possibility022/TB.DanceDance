using Application.Features.AccessManagement;
using Application.Features.Events;
using Application.Features.Sharing;
using Application.Features.Videos;
﻿using Application.Features.Groups;
﻿using Application.Features.Comments;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services
                .AddFastEndpoints(o => o.Assemblies = [typeof(DependencyInjection).Assembly])
                // Generates an OpenAPI document from the endpoint request/response types.
                // The JWT bearer security scheme is added automatically (matches our Bearer auth).
                .SwaggerDocument(o =>
                {
                    // Use short (unqualified) type names as schema ids so generated client/model
                    // names are e.g. "MyVideosResponse" rather than the full namespace-flattened name.
                    o.ShortSchemaNames = true;
                    o.DocumentSettings = s =>
                    {
                        s.DocumentName = "v1";
                        s.Title = "TB.DanceDance API";
                        s.Version = "v1";
                    };
                });

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

        // Serve the Swagger/OpenAPI UI + JSON only in Development.
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        if (env.IsDevelopment())
        {
            app.UseSwaggerGen();
        }

        return app;
    }
}
