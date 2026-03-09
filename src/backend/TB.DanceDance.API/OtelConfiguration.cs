using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TB.DanceDance.API;

public static class OtelConfiguration
{
    public static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration.ReadFrom.Configuration(configuration);
        Log.Logger = loggerConfiguration.CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger);
        });
    }
    
    public static void ConfigureOpenTelemetryAndLogging(
        IServiceCollection services
        , IConfiguration configuration
        , IHostEnvironment environment
        , ILoggingBuilder logging
        )
    {
        const string serviceName = "DanceDance.API";
        const string serviceVersion = "1.0.0";
        
        var config = configuration.GetSection(OTelOptions.Position);
        var otelOptions = config.Get<OTelOptions>();
        
        if (otelOptions is null)
        {
            ConfigureLogging(services, configuration);
            return;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                })
            )
            .WithTracing(tracing => tracing
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(options => { 
                    options.Endpoint = new Uri(otelOptions.OTelOrigin);
                    options.Protocol = OtlpExportProtocol.Grpc;
                    if (string.IsNullOrEmpty(otelOptions.IngresKey))
                        options.Headers = $"signoz-ingestion-key={otelOptions.IngresKey}";
                })
            )
            .WithMetrics(metrics => metrics
                .AddMeter(serviceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddOtlpExporter(options => { 
                    options.Endpoint = new Uri(otelOptions.OTelOrigin);
                    options.Protocol = OtlpExportProtocol.Grpc;
                    if (string.IsNullOrEmpty(otelOptions.IngresKey))
                        options.Headers = $"signoz-ingestion-key={otelOptions.IngresKey}";
                }))
            ;

        logging.ClearProviders();
        logging.AddOpenTelemetry(opt =>
        {
            opt.IncludeFormattedMessage = true; // Include the formatted log message
            opt.IncludeScopes = true; // Include scope information
            opt.ParseStateValues = true; // Enable structured log parsing
            opt.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName)); // Match the trace service name
            opt.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelOptions.OTelOrigin);
                options.Protocol = OtlpExportProtocol.Grpc;
                if (string.IsNullOrEmpty(otelOptions.IngresKey))
                    options.Headers = $"signoz-ingestion-key={otelOptions.IngresKey}";
            });
        });
    }
}