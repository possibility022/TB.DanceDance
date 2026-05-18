using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Videos;

public static class VideosModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddVideosFeature()
        {
            services.AddScoped<IVideoService, VideoService>();
            services.AddScoped<IVideoUploaderService, VideoUploaderService>();
            return services;
        }
    }
}
