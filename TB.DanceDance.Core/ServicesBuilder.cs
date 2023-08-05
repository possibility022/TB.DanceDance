﻿using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Services;

namespace TB.DanceDance.Core;

public static class ServicesBuilder
{
    public static IServiceCollection ConfigureVideoServices(this IServiceCollection services,
        string connectionString,
        Func<IServiceProvider, IVideoFileLoader>? videoFileLoader = null)
    {

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        services
            .AddSingleton<IBlobDataServiceFactory>(r => new BlobDataServiceFactory(connectionString))
            .AddScoped<IVideoService, VideoService>()
            .AddScoped<IEventService, EventService>()
            .AddScoped<IGroupService, GroupService>()
            .AddScoped<IVideoUploaderService, VideoUploaderService>();

        if (videoFileLoader != null)
        {
            services.AddScoped<IVideoFileLoader>(videoFileLoader);
        }
        else
        {
            services.AddSingleton<IVideoFileLoader, FakeFileLoader>();
        }

        return services;
    }
}