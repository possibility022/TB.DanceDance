using Application.Features.Sharing;
﻿using Application.Features.Groups;
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
                .AddScoped<IVideoUploaderService, VideoUploaderService>()
                .AddScoped<ICommentService, CommentService>();
                .AddScoped<ISharedLinkService, SharedLinkService>();

            services.AddCommentsFeature();

            services.AddSharingFeature();
            services.AddGroupsFeature();

            return services;
        }
    }
}