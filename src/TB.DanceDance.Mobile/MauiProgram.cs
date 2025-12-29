using CommunityToolkit.Maui;
using Duende.IdentityModel.OidcClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Threading.Channels;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;
using TB.DanceDance.Mobile.PageModels;
using TB.DanceDance.Mobile.Pages;
using TB.DanceDance.Mobile.Services.Auth;

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

        var serilogConfig = new LoggerConfiguration();


#if DEBUG
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        serilogConfig.WriteTo.Debug();
        serilogConfig.WriteTo.File(Path.Combine(FileSystem.Current.AppDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug);
#if ANDROID
        serilogConfig
            .WriteTo.AndroidLog()
            .Enrich.WithProperty(Serilog.Core.Constants.SourceContextPropertyName, "tb.dancedance.mobile");
#endif
#else
        serilogConfig.WriteTo.File(Path.Combine(FileSystem.Current.AppDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information);
#endif

        Log.Logger = serilogConfig.CreateLogger();
        builder.Services.AddSerilog(Log.Logger);

        builder.Services.AddSingleton<MainPageViewModel>();

        var networker = new NetworkStatusMonitor();
        builder.Services.AddSingleton<NetworkStatusMonitor>(networker);
        builder.Services.AddScoped<UploadWorker>();
#if ANDROID
        builder.Services.AddSingleton<IPlatformNotification, UploadForegroundService>();
#endif
        builder.Services.AddSingleton(Channel.CreateUnbounded<UploadProgressEvent>());

        builder.Services.AddTransient<VideoProvider>();
        builder.Services.AddTransient<IMauiInitializeService, DataStorageInitialize>();

        builder.Services.AddSingleton<EventsPageModel>();
        builder.Services.AddSingleton<MyVideosPageModel>();
        builder.Services.AddSingleton<GroupVideosPageModel>();
        builder.Services.AddSingleton<UploadManagerPageModel>();

        var browserFactory = new BrowserFactory();
        browserFactory.SetFactory(() => new MauiAuthenticationBrowser());

        var networkAddressResolver = new NetworkAddressResolver(DeviceInfo.Platform);
        builder.Services.AddSingleton(networkAddressResolver);

        builder.Services.AddSingleton<IBrowserFactory>(browserFactory);

        var authSettingsFactory = new AuthSettingsFactory(browserFactory, networkAddressResolver, DeviceInfo.Platform);
        builder.Services.AddSingleton(authSettingsFactory);

        var handler = DanceApiHttpClientFactory.CreateBaseHttpMessageHandlerChain(networkAddressResolver);

        var primaryOptions = authSettingsFactory.GetClientOptions(handler, DanceApiHttpClientFactory.ApiMainUrl);
        var secondaryOptions = authSettingsFactory.GetClientOptions(handler, DanceApiHttpClientFactory.ApiBackupUrl);

        var primaryTokenStorage = new TokenStorage(TokenStorage.PrimaryStorageKey);
        var secondaryTokenStorage = new TokenStorage(TokenStorage.SecondaryStorageKey);

        builder.Services.AddKeyedSingleton(TokenStorage.PrimaryStorageKey, primaryTokenStorage);
        builder.Services.AddKeyedSingleton(TokenStorage.SecondaryStorageKey, secondaryTokenStorage);

        var primaryTokenProvider = new TokenProviderService(new OidcClient(primaryOptions), primaryTokenStorage);
        var secondaryTokenProvider = new TokenProviderService(new OidcClient(secondaryOptions), secondaryTokenStorage);

        builder.Services.AddKeyedSingleton<ITokenProviderService>(TokenStorage.PrimaryStorageKey, primaryTokenProvider);
        builder.Services.AddKeyedSingleton<ITokenProviderService>(TokenStorage.SecondaryStorageKey, secondaryTokenProvider);

        builder.Services.AddTransientWithShellRoute<EventDetailsPage, EventDetailsPageModel>(Routes.Events.EventDetails);
        builder.Services.AddTransientWithShellRoute<AddEventPage, AddEventPageModel>(Routes.Events.Add);
        builder.Services.AddTransientWithShellRoute<GetAccessPage, GetAccessPageModel>(Routes.GetAccess);
        builder.Services.AddTransientWithShellRoute<WatchVideo, WatchVideoPageModel>(Routes.Player);
        builder.Services.AddTransientWithShellRoute<UploadVideoPage, UploadVideoPageModel>(Routes.Upload.Uploader);


        builder.Services.AddDbContext<VideosDbContext>(options =>
        {
            options.UseSqlite(Constants.VideosDatabasePath);
        });

        builder.Services.AddTransient<IHttpClientFactory, DanceApiHttpClientFactory>();

        builder.Services.AddTransient<IVideoUploader, VideoUploader>();
        builder.Services.AddScoped<IDanceHttpApiClient, DanceHttpApiClient>();


        return builder.Build();
    }
}
