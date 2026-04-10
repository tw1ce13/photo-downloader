using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using PhotoDownloader.Services.Interfaces;

namespace PhotoDownloader.Services.Implementations;

/// <summary>
/// Реализация загрузки через <see cref="HttpClient"/> с потоковым чтением и отменой.
/// </summary>
public sealed class ImageDownloadService : IImageDownloadService
{
    private const double DownloadWeight = 0.92;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ImageSource> DownloadAsync(Uri uri, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET {Uri}", uri);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;
            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var initialCapacity = 64 * 1024;
            if (total is > 0)
                initialCapacity = (int)Math.Min(int.MaxValue, total.Value);

            await using var buffer = new MemoryStream(initialCapacity);
            var rented = ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                int read;
                long readTotal = 0;
                while ((read = await networkStream.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await buffer.WriteAsync(rented.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    readTotal += read;
                    ReportDownloadProgress(progress, readTotal, total);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            buffer.Position = 0;
            progress?.Report(DownloadWeight);

            var image = await Task.Run(() => DecodeBitmap(buffer), cancellationToken).ConfigureAwait(false);
            progress?.Report(1.0);

            _logger.LogInformation(
                "Изображение получено {Uri}, размер данных {Bytes} байт, Content-Length: {ContentLength}",
                uri,
                buffer.Length,
                total);

            return image;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Загрузка прервана (отмена) {Uri}", uri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении изображения {Uri}", uri);
            throw;
        }
    }

    private static void ReportDownloadProgress(IProgress<double>? progress, long readTotal, long? total)
    {
        if (progress is null)
            return;

        if (total is > 0)
        {
            var ratio = Math.Min(1.0, (double)readTotal / total.Value);
            progress.Report(ratio * DownloadWeight);
            return;
        }

        // Без Content-Length: плавное приближение к верхней границе фазы загрузки.
        var estimate = 1.0 - Math.Exp(-readTotal / (5.0 * 1024 * 1024));
        progress.Report(estimate * DownloadWeight * 0.98);
    }

    private static BitmapImage DecodeBitmap(MemoryStream stream)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
