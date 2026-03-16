using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TB.DanceDance.API;

public static class OtelConfiguration
{
    public const string ServiceName = "DanceDance.API";
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
    
    public static void ConfigureOpenTelemetryAndLogging(
        IServiceCollection services, IHostEnvironment environment
        , ILoggingBuilder logging
        )
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureDefaultResources(resource, environment.EnvironmentName))
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
                .AddNpgsqlInstrumentation())
            ;
        
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddOpenTelemetry(opt =>
        {
            var builder = ResourceBuilder.CreateDefault();
            ConfigureDefaultResources(builder, environment.EnvironmentName);
            opt.SetResourceBuilder(builder);
        });
    }
}