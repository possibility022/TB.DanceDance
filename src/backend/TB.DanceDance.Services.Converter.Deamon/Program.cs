using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Runtime.CompilerServices;
using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;
[assembly: InternalsVisibleTo("TB.DanceDance.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

const string ServiceName = "DanceDance.ConverterDeamon";
const string ServiceVersion = "1.0.0";

var ConfigureDefaultResources = (ResourceBuilder resourceBuilder, string environment) =>
{
    resourceBuilder.AddService(
        serviceName: ServiceName,
        serviceVersion: ServiceVersion,
        serviceInstanceId: Environment.MachineName
    ).AddAttributes(new Dictionary<string, object> { ["deployment.environment"] = environment, });
};

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

if (builder.Environment.IsProduction())
    builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true);

if (builder.Environment.IsEnvironment("QA"))
    builder.Configuration.AddJsonFile("appsettings.QA.json", optional: true);

builder.Configuration.AddEnvironmentVariables(prefix: "tb_dancedance_converter_");

var config = new ProgramConfig();
builder.Configuration.GetSection("ConverterConfig").Bind(config);

var loggerConfiguration = new Serilog.LoggerConfiguration();
loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
var logger = loggerConfiguration.CreateLogger();
Log.Logger = logger;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => ConfigureDefaultResources(resource, builder.Environment.EnvironmentName))
    .UseOtlpExporter()
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
    )
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
    );

config.ConfigureFfmpeg();
config.Validate();

builder.Logging.ClearProviders();
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<OAuthHttpClient>((s) =>
{
    var programConfig = s.GetRequiredService<ProgramConfig>();
    return new OAuthHttpClient()
    {
        BaseAddress = new Uri(programConfig.OAuthOrigin)
    };
});

builder.Services.AddSingleton<ApiHttpClient>((s) =>
{
    var programConfig = s.GetRequiredService<ProgramConfig>();
    var tokenProvider = new TokenProvider(s.GetRequiredService<OAuthHttpClient>(), programConfig.TokenProviderOptions);
    var handler = new TokenHttpHandler(tokenProvider);

    var apiHttpClient = new ApiHttpClient(handler, true)
    {
        Timeout = TimeSpan.FromSeconds(60 * 5),
        BaseAddress = new Uri(programConfig.ApiOrigin)
    };

    return apiHttpClient;
});

builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<IDanceDanceApiClient, DanceDanceApiClient>();
builder.Services.AddScoped<IFFmpegClientConverter, FFmpegClientConverter>();

builder.Services.AddHostedService<Deamon>();

IHost host = builder.Build();
host.Run();


