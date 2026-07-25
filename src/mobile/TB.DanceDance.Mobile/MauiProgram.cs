using CommunityToolkit.Maui;
using Duende.IdentityModel.OidcClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Nalu;
using Serilog;
using Serilog.Events;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;
using TB.DanceDance.Mobile.Pages;
using TB.DanceDance.Mobile.Pages.Access;
using TB.DanceDance.Mobile.Pages.Account;
using TB.DanceDance.Mobile.Pages.Events;
using TB.DanceDance.Mobile.Pages.Groups;
using TB.DanceDance.Mobile.Pages.Popups;
using TB.DanceDance.Mobile.Pages.Upload;
using TB.DanceDance.Mobile.Pages.WatchVideos;
using AccountPageModel = TB.DanceDance.Mobile.Pages.Account.AccountPageModel;
using AddEventPageModel = TB.DanceDance.Mobile.Pages.Events.AddEventPageModel;
using EventDetailsPageModel = TB.DanceDance.Mobile.Pages.Events.EventDetailsPageModel;
using EventsPageModel = TB.DanceDance.Mobile.Pages.Events.EventsPageModel;
using GetAccessPageModel = TB.DanceDance.Mobile.Pages.Access.GetAccessPageModel;
using GroupVideosPageModel = TB.DanceDance.Mobile.Pages.Groups.GroupVideosPageModel;
using MainPageViewModel = TB.DanceDance.Mobile.Pages.MainPageViewModel;
using MyVideosPageModel = TB.DanceDance.Mobile.Pages.MyVideosPageModel;
using UploadManagerPageModel = TB.DanceDance.Mobile.Pages.Upload.UploadManagerPageModel;
using UploadVideoPageModel = TB.DanceDance.Mobile.Pages.Upload.UploadVideoPageModel;
using WatchVideoPageModel = TB.DanceDance.Mobile.Pages.WatchVideos.WatchVideoPageModel;

namespace TB.DanceDance.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement(false)
            .UseMauiCommunityToolkit()
            .UseNaluLayouts()
            .UseNaluTabBar()
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

        builder.Services.AddScoped<UploadWorker>();
        builder.Services.AddSingleton<UploadExecutionGate>();
        builder.Services.AddSingleton<IUploadQueueChangeNotifier, UploadQueueChangeNotifier>();
        builder.Services.AddSingleton<IUploadNetworkSettings, UploadNetworkSettings>();
#if ANDROID
        builder.Services.AddSingleton<IUploadScheduler, AndroidUploadScheduler>();
#else
        builder.Services.AddSingleton<IUploadScheduler, InProcessUploadScheduler>();
#endif
        builder.Services.AddSingleton(new UploadQueueOptions
        {
            StagingDirectory = Path.Combine(FileSystem.AppDataDirectory, "upload-queue")
        });
        builder.Services.AddScoped<IUploadQueueService, UploadQueueService>();

        builder.Services.AddTransient<VideoProvider>();
        builder.Services.AddSingleton<DataStorageInitialize>();
        builder.Services.AddSingleton<IUploadStoreInitializer>(services =>
            services.GetRequiredService<DataStorageInitialize>());

        var networkAddressResolver = new NetworkAddressResolver(DeviceInfo.Platform);
        builder.Services.AddSingleton(networkAddressResolver);

        var browserFactory = new BrowserFactory();
        browserFactory.SetFactory(() => new MauiAuthenticationBrowser(networkAddressResolver));

        builder.Services.AddSingleton<IBrowserFactory>(browserFactory);

        var authSettingsFactory = new AuthSettingsFactory(browserFactory, networkAddressResolver, DeviceInfo.Platform);
        builder.Services.AddSingleton(authSettingsFactory);

        builder.Services.AddKeyedSingleton<TokenStorage>(
            TokenStorage.PrimaryStorageKey,
            (_, _) => new TokenStorage(TokenStorage.PrimaryStorageKey));
        builder.Services.AddKeyedSingleton<ITokenProviderService>(
            TokenStorage.PrimaryStorageKey,
            (services, _) =>
            {
                var resolver = services.GetRequiredService<NetworkAddressResolver>();
                var settingsFactory = services.GetRequiredService<AuthSettingsFactory>();
                var handler = DanceApiHttpClientFactory.CreateBaseHttpMessageHandlerChain(resolver);
                var options = settingsFactory.GetClientOptions(handler, DanceApiHttpClientFactory.AuthMainUrl);
                var storage = services.GetRequiredKeyedService<TokenStorage>(TokenStorage.PrimaryStorageKey);
                return new TokenProviderService(new OidcClient(options), storage);
            });

        // Popups
        builder.Services.AddTransientPopup<SharingPopup, SharingPopupViewModel>();


        builder.Services.AddDbContextFactory<VideosDbContext>(options =>
        {
            options.UseSqlite(Constants.VideosDatabasePath);
        });

        builder.Services.AddSingleton<IHttpClientFactory, DanceApiHttpClientFactory>();

        builder.Services.AddTransient<IVideoUploader, VideoUploader>();
        builder.Services.AddSingleton<UserAccessCache>();
        builder.Services.AddScoped<IDanceHttpApiClient, DanceHttpApiClient>();


        return builder.Build();
    }
}
