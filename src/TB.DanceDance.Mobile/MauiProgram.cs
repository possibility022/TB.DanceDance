using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.PageModels;
using TB.DanceDance.Mobile.Pages;
using TB.DanceDance.Mobile.Services.Auth;
using TB.DanceDance.Mobile.Services.DanceApi;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement()
            .UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif


        builder.Services.AddSingleton<EventsPageModel>();
        builder.Services.AddSingleton<GroupVideosPageModel>();
        builder.Services.AddSingleton<ITokenProviderService>(new StorageTokenProviderService());
        
        builder.Services.AddTransientWithShellRoute<EventDetailsPage, EventDetailsPageModel>("eventDetails");
        builder.Services.AddTransientWithShellRoute<WatchVideo, WatchVideoPageModel>("watchVideo");
        
        
        builder.Services.AddDbContext<VideosDbContext>(options =>
        {
            options.UseSqlite(Constants.VideosDatabasePath);
        });

        builder.Services.AddTransient<IHttpClientFactory,HttpClientFactory>();
        
        builder.Services.AddTransient<VideoUploader>();
        builder.Services.AddTransient<BlobUploader>();
        builder.Services.AddScoped<DanceHttpApiClient>();


		return builder.Build();
	}
}
