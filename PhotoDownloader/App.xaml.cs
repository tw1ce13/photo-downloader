using System.Net;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoDownloader.Infrastructure;
using PhotoDownloader.Options;
using PhotoDownloader.Services;
using PhotoDownloader.Services.Implementations;
using PhotoDownloader.Services.Interfaces;
using PhotoDownloader.ViewModels;
using PhotoDownloader.Views;
using Serilog;

namespace PhotoDownloader;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .UseSerilog(SerilogConfiguration.ConfigureHostLogging)
            .ConfigureServices(static (_, services) =>
            {
                services.AddOptions<ImageDownloadOptions>();

                services.AddHttpClient<IImageDownloadService, ImageDownloadService>(static (_, client) =>
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("PhotoDownloader/1.0");
                    client.Timeout = TimeSpan.FromMinutes(30);
                }).ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                });

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        Log.Information("Приложение запущено");

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync();
        _host?.Dispose();
        Log.Information("Приложение завершено");
        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
