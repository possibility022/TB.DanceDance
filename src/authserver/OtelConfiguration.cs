using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TB.Auth.Web;

public static class OtelConfiguration
{
    public const string ServiceName = "DanceDance.Auth";
    public const string ServiceVersion = "1.0.0";

    private static void ConfigureDefaultResources(ResourceBuilder resourceBuilder, string environment)
    {
        resourceBuilder.AddService(
            serviceName: ServiceName,
            serviceVersion: ServiceVersion,
            serviceInstanceId: Environment.MachineName
        ).AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = environment,
        });
    }

    public static void ConfigureOpenTelemetryAndLogging(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureDefaultResources(resource, builder.Environment.EnvironmentName))
            .UseOtlpExporter()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
            )
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsqlInstrumentation());

        builder.Logging.ClearProviders();

        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
        var logger = loggerConfiguration.CreateLogger();
        Log.Logger = logger;

        builder.Logging.AddSerilog(logger);
    }
}
