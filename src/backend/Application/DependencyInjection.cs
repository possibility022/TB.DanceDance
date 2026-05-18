using Application.Features.Sharing;
﻿using Application.Features.Comments;
using Application.Services;
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
                .AddScoped<IAccessService, AccessService>()
                .AddScoped<IVideoService, VideoService>()
                .AddScoped<IEventService, EventService>()
                .AddScoped<IGroupService, GroupService>()
                .AddScoped<IVideoUploaderService, VideoUploaderService>()
                .AddScoped<ICommentService, CommentService>();
                .AddScoped<ISharedLinkService, SharedLinkService>();

            services.AddCommentsFeature();

            services.AddSharingFeature();

            return services;
        }
    }
}