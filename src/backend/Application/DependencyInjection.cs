using Application.Features.AccessManagement;
using Application.Features.Events;
using Application.Features.Sharing;
using Application.Features.Videos;
﻿using Application.Features.Groups;
﻿using Application.Features.Comments;
using Domain.Services;
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

            return services;
        }
    }
}