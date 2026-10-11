using System.Globalization;
using Serilog;
using Serilog.Formatting.Compact;

namespace FixtureHub.Api.Observability;

internal static class LoggingServices
{
    public const string JsonConsoleKey = "Observability:JsonConsole";

    private const string TextTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {TraceId}{NewLine}{Exception}";

    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSerilog(
            (provider, logger) =>
            {
                logger
                    .ReadFrom.Configuration(configuration)
                    .ReadFrom.Services(provider);

                if (configuration.GetValue<bool>(JsonConsoleKey))
                    logger.WriteTo.Console(new RenderedCompactJsonFormatter());
                else
                    logger.WriteTo.Console(outputTemplate: TextTemplate, formatProvider: CultureInfo.InvariantCulture);
            },
            preserveStaticLogger: true);
}
