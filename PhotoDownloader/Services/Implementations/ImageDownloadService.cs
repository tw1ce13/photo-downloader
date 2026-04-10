using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoDownloader.Options;
using PhotoDownloader.Services.Interfaces;

namespace PhotoDownloader.Services.Implementations;

/// <summary>
/// Реализация загрузки через <see cref="HttpClient"/> с потоковым чтением и отменой.
/// В продакшене стоит дополнительно ограничить целевые хосты (защита от SSRF при доверенном вводе).
/// </summary>
public sealed class ImageDownloadService : IImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ImageDownloadOptions _options;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(
        HttpClient httpClient,
        IOptions<ImageDownloadOptions> options,
        ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
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

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(mediaType) &&
                !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Ответ с неожиданным Content-Type {MediaType} для {Uri} — продолжаем загрузку",
                    mediaType,
                    uri);
            }

            var total = response.Content.Headers.ContentLength;
            if (total is > 0 && total.Value > _options.MaxDownloadBytes)
            {
                throw new InvalidOperationException(
                    $"Размер файла по заголовку ({total.Value} байт) превышает лимит {_options.MaxDownloadBytes} байт.");
            }

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var initialCapacity = 64 * 1024;
            if (total is > 0)
                initialCapacity = (int)Math.Min(int.MaxValue, Math.Min(total.Value, _options.MaxDownloadBytes));

            await using var buffer = new MemoryStream(initialCapacity);
            var rented = ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                int read;
                long readTotal = 0;
                while ((read = await networkStream.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    readTotal += read;
                    if (readTotal > _options.MaxDownloadBytes)
                    {
                        throw new InvalidOperationException(
                            $"Превышен максимальный размер загрузки ({_options.MaxDownloadBytes} байт).");
                    }

                    await buffer.WriteAsync(rented.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    DownloadProgressReporter.Report(progress, readTotal, total);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            buffer.Position = 0;
            progress?.Report(DownloadProgressReporter.DownloadWeight);

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
