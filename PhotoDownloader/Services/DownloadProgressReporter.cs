namespace PhotoDownloader.Services;

/// <summary>
/// Доля 0–1 для фазы скачивания (до декодирования). Оставшаяся часть до 1.0 — декодирование.
/// </summary>
public static class DownloadProgressReporter
{
    public const double DownloadWeight = 0.92;

    public static void Report(IProgress<double>? progress, long readTotal, long? contentLength)
    {
        if (progress is null)
            return;

        if (contentLength is > 0)
        {
            var ratio = Math.Min(1.0, (double)readTotal / contentLength.Value);
            progress.Report(ratio * DownloadWeight);
            return;
        }

        var estimate = 1.0 - Math.Exp(-readTotal / (5.0 * 1024 * 1024));
        progress.Report(estimate * DownloadWeight * 0.98);
    }
}
