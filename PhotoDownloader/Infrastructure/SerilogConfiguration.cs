using System.IO;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace PhotoDownloader.Infrastructure;

internal static class SerilogConfiguration
{
    public static void ConfigureHostLogging(HostBuilderContext context, IServiceProvider _, LoggerConfiguration configuration)
    {
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoDownloader",
            "logs");
        Directory.CreateDirectory(logsDir);

        configuration
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(logsDir, "photo-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true);
    }
}
