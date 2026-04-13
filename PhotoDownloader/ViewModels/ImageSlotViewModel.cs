using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoDownloader.Infrastructure;
using PhotoDownloader.Services.Interfaces;

namespace PhotoDownloader.ViewModels;

/// <summary>
/// Одна «ячейка»: URL, превью, старт/стоп загрузки и локальный прогресс (0–100) для общей шкалы.
/// </summary>
public partial class ImageSlotViewModel : ObservableObject
{
    private readonly IImageDownloadService _downloadService;
    private readonly ILogger<ImageSlotViewModel> _logger;
    private readonly Action _onSlotStateChanged;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _cts;

    public ImageSlotViewModel(
        int slotNumber,
        IImageDownloadService downloadService,
        ILogger<ImageSlotViewModel> logger,
        Action onSlotStateChanged)
    {
        SlotNumber = slotNumber;
        _downloadService = downloadService;
        _logger = logger;
        _onSlotStateChanged = onSlotStateChanged;
        _dispatcher = Application.Current.Dispatcher;
    }

    public int SlotNumber { get; }

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private ImageSource? _previewImage;

    [ObservableProperty]
    private bool _isDownloading;

    /// <summary>
    /// Вклад слота в общий прогресс, 0–100.
    /// </summary>
    [ObservableProperty]
    private double _slotProgress;

    [ObservableProperty]
    private string? _statusMessage;

    partial void OnUrlChanged(string value)
    {
        StartDownloadCommand.NotifyCanExecuteChanged();
        _onSlotStateChanged();
    }

    partial void OnIsDownloadingChanged(bool value)
    {
        StartDownloadCommand.NotifyCanExecuteChanged();
        StopDownloadCommand.NotifyCanExecuteChanged();
        _onSlotStateChanged();
    }

    partial void OnSlotProgressChanged(double value) => _onSlotStateChanged();

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartDownloadAsync()
    {
        if (!ImageUrlValidator.TryValidate(Url, out var uri, out var validationError))
        {
            _logger.LogWarning("Слот {Slot}: отклонён URL «{Url}» — {Reason}", SlotNumber, Url, validationError);
            StatusMessage = validationError;
            return;
        }

        await _dispatcher.InvokeAsync(PrepareForNewDownload);

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            IsDownloading = true;
            StatusMessage = null;
            SetSlotProgress(0);

            _logger.LogInformation("Слот {Slot}: начало загрузки {Uri}", SlotNumber, uri);

            var progress = new Progress<double>(fraction => SetSlotProgress(fraction * 100));

            var image = await _downloadService.DownloadAsync(uri, progress, token).ConfigureAwait(true);

            PreviewImage = image;
            StatusMessage = null;
            SetSlotProgress(100);
            _logger.LogInformation("Слот {Slot}: загрузка успешно завершена", SlotNumber);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            StatusMessage = "Остановлено";
            SetSlotProgress(0);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Превышено время ожидания ответа или соединение прервано.";
            SetSlotProgress(0);
        }
        catch (Exception ex)
        {
            StatusMessage = DownloadUserMessage.From(ex);
            SetSlotProgress(0);
        }
        finally
        {
            IsDownloading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void StopDownload()
    {
        _logger.LogInformation("Слот {Slot}: запрошена остановка загрузки", SlotNumber);
        _cts?.Cancel();
    }

    private bool CanStart() => !IsDownloading && !string.IsNullOrWhiteSpace(Url);

    private bool CanStop() => IsDownloading;

    private void PrepareForNewDownload()
    {
        PreviewImage = null;
        SlotProgress = 0;
    }

    private void SetSlotProgress(double value)
    {
        void Apply() => SlotProgress = Math.Clamp(value, 0, 100);

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
