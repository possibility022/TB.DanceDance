using CommunityToolkit.Maui;
using Duende.IdentityModel.OidcClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Nalu;
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
using TB.DanceDance.Mobile.Pages.Popups;
using TB.DanceDance.Mobile.Services.Auth;

namespace TB.DanceDance.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        

        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if ANDROID
    events.AddAndroid(android => android
        .OnResume(e => ManageUploading())
        .OnCreate((e,x) => ManageUploading()));
#endif
            })
            .UseMauiCommunityToolkitMediaElement(false)
            .UseMauiCommunityToolkit()
            .UseNaluNavigation<App>(nav => nav
                .AddPage<MainPageViewModel, MainPage>()
                .AddPage<EventsPageModel, EventsPage>()
                .AddPage<GroupVideosPageModel, GroupVideosPage>()
                .AddPage<MyVideosPageModel, MyVideosPage>()
                .AddPage<UploadManagerPageModel, UploadManagerPage>()
                .AddPage<EventDetailsPageModel, EventDetailsPage>()
                .AddPage<AddEventPageModel, AddEventPage>()
                .AddPage<GetAccessPageModel, GetAccessPage>()
                .AddPage<WatchVideoPageModel, WatchVideo>()
                .AddPage<UploadVideoPageModel, UploadVideoPage>()
                .AddPage<AccountPageModel, AccountPage>()
                .WithLeakDetectorState(NavigationLeakDetectorState.EnabledWithDebugger))
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Round.otf", "MaterialRound");
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

        var networker = new NetworkStatusMonitor();
        builder.Services.AddSingleton<NetworkStatusMonitor>(networker);
        builder.Services.AddScoped<UploadWorker>();
#if ANDROID
        builder.Services.AddSingleton<IPlatformNotification, UploadForegroundService>();
#endif
        builder.Services.AddSingleton(Channel.CreateUnbounded<UploadProgressEvent>());

        builder.Services.AddTransient<VideoProvider>();
        builder.Services.AddTransient<IMauiInitializeService, DataStorageInitialize>();

        var networkAddressResolver = new NetworkAddressResolver(DeviceInfo.Platform);
        builder.Services.AddSingleton(networkAddressResolver);

        var browserFactory = new BrowserFactory();
        browserFactory.SetFactory(() => new MauiAuthenticationBrowser(networkAddressResolver));

        builder.Services.AddSingleton<IBrowserFactory>(browserFactory);

        var authSettingsFactory = new AuthSettingsFactory(browserFactory, networkAddressResolver, DeviceInfo.Platform);
        builder.Services.AddSingleton(authSettingsFactory);

        var handler = DanceApiHttpClientFactory.CreateBaseHttpMessageHandlerChain(networkAddressResolver);

        var primaryOptions = authSettingsFactory.GetClientOptions(handler, DanceApiHttpClientFactory.AuthMainUrl);

        var primaryTokenStorage = new TokenStorage(TokenStorage.PrimaryStorageKey);

        builder.Services.AddKeyedSingleton(TokenStorage.PrimaryStorageKey, primaryTokenStorage);

        var primaryTokenProvider = new TokenProviderService(new OidcClient(primaryOptions), primaryTokenStorage);

        builder.Services.AddKeyedSingleton<ITokenProviderService>(TokenStorage.PrimaryStorageKey, primaryTokenProvider);

        // Popups
        builder.Services.AddTransientPopup<SharingPopup, SharingPopupViewModel>();


        builder.Services.AddDbContext<VideosDbContext>(options =>
        {
            options.UseSqlite(Constants.VideosDatabasePath);
        });

        builder.Services.AddTransient<IHttpClientFactory, DanceApiHttpClientFactory>();

        builder.Services.AddTransient<IVideoUploader, VideoUploader>();
        builder.Services.AddScoped<IDanceHttpApiClient, DanceHttpApiClient>();


        return builder.Build();
    }

    private static void ManageUploading()
    {
#if ANDROID
        NetworkStatusMonitor.ManageBackgroundService(Connectivity.Current.NetworkAccess, Connectivity.Current.ConnectionProfiles);
#endif
    }
}
