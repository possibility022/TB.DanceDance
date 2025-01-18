using CommunityToolkit.Maui;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using TB.DanceDance.Mobile.Services.Auth;

namespace TB.DanceDance.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionToolkit()
            .ConfigureMauiHandlers(handlers =>
            {
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddLogging(configure => configure.AddDebug());
#endif

        builder.Services.AddSingleton<ProjectRepository>();
        builder.Services.AddSingleton<TaskRepository>();
        builder.Services.AddSingleton<CategoryRepository>();
        builder.Services.AddSingleton<TagRepository>();
        builder.Services.AddSingleton<SeedDataService>();
        builder.Services.AddSingleton<ModalErrorHandler>();
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<ProjectListPageModel>();
        builder.Services.AddSingleton<ManageMetaPageModel>();

        builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
        builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
        builder.Services.AddTransient<HttpClientHandlerFactory>();
        
        ConfigureOauth(builder.Services);

        return builder.Build();
    }

    private static void ConfigureOauth(IServiceCollection builderServices)
    {
        builderServices.AddTransient<OidcClient>((services) =>
        {
            var handlerFactory = services.GetRequiredService<HttpClientHandlerFactory>();
            var handler = handlerFactory.GetHttpClientHandler();
            var options = AuthSettingsFactory.GetClientOptions(handler);

            return new OidcClient(options);
        });

        builderServices.AddScoped<ITokenProviderService, TokenProviderService>();
    }
}