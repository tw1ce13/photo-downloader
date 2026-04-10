namespace PhotoDownloader.Options;

/// <summary>
/// Параметры загрузки изображений по HTTP.
/// </summary>
public sealed class ImageDownloadOptions
{
    private const long DefaultMaxDownloadBytes = 50L * 1024 * 1024;

    /// <summary>
    /// Максимальный размер тела ответа в байтах.
    /// </summary>
    public long MaxDownloadBytes { get; set; } = DefaultMaxDownloadBytes;
}
