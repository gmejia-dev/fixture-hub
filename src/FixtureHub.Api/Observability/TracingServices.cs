using FixtureHub.Application.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FixtureHub.Api.Observability;

internal static class TracingServices
{
    public const string ServiceName = "fixturehub-api";
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static IServiceCollection AddTracing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ApplicationTracing.SourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddSqlClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]))
                    tracing.AddOtlpExporter();
            });

        return services;
    }
}
