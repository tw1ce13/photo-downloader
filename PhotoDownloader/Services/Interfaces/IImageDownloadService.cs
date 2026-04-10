using System.Windows.Media;

namespace PhotoDownloader.Services.Interfaces;

/// <summary>
/// Загрузка изображения по HTTP с отчётом о прогрессе и отменой.
/// </summary>
public interface IImageDownloadService
{
    /// <summary>
    /// Скачивает и декодирует изображение. Декодирование выполняется вне UI-потока.
    /// </summary>
    /// <param name="uri">Адрес ресурса.</param>
    /// <param name="progress">Доля выполнения от 0 до 1 (загрузка + декодирование).</param>
    /// <param name="cancellationToken">Отмена.</param>
    Task<ImageSource> DownloadAsync(Uri uri, IProgress<double>? progress, CancellationToken cancellationToken);
}
