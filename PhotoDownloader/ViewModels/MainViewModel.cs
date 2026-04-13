using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoDownloader.Services;
using PhotoDownloader.Services.Interfaces;

namespace PhotoDownloader.ViewModels;

/// <summary>
/// Главная модель: три независимых слота и агрегированный прогресс.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher = Application.Current.Dispatcher;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(IImageDownloadService downloadService, ILogger<MainViewModel> logger, ILogger<ImageSlotViewModel> slotLogger)
    {
        _logger = logger;
        Slots = new[]
        {
            new ImageSlotViewModel(1, downloadService, slotLogger, ScheduleOverallRefresh),
            new ImageSlotViewModel(2, downloadService, slotLogger, ScheduleOverallRefresh),
            new ImageSlotViewModel(3, downloadService, slotLogger, ScheduleOverallRefresh),
        };

        RefreshOverall();
    }

    public IReadOnlyList<ImageSlotViewModel> Slots { get; }

    public ImageSlotViewModel Slot1 => Slots[0];
    public ImageSlotViewModel Slot2 => Slots[1];
    public ImageSlotViewModel Slot3 => Slots[2];

    /// <summary>
    /// Средняя доля выполнения по слотам, где заполнен URL, 0-100.
    /// </summary>
    [ObservableProperty]
    private double _overallProgress;

    /// <summary>Число слотов, у которых сейчас идёт загрузка.</summary>
    [ObservableProperty]
    private int _activeDownloadCount;

    [ObservableProperty]
    private string _overallStatusText = "Активных загрузок: 0";

    [RelayCommand]
    private void DownloadAll()
    {
        _logger.LogInformation("Команда «Скачать все»");

        foreach (var slot in Slots)
        {
            if (slot.StartDownloadCommand.CanExecute(null))
                slot.StartDownloadCommand.Execute(null);
        }
    }

    private void ScheduleOverallRefresh()
    {
        if (_dispatcher.CheckAccess())
            RefreshOverall();
        else
            _dispatcher.BeginInvoke(RefreshOverall, DispatcherPriority.Background);
    }

    private void RefreshOverall()
    {
        var requestedSlots = Slots.Where(s => !string.IsNullOrWhiteSpace(s.Url)).ToArray();

        ActiveDownloadCount = requestedSlots.Count(s => s.IsDownloading);
        OverallProgress = requestedSlots.Length == 0
            ? 0
            : requestedSlots.Average(s => s.SlotProgress);
        OverallStatusText = $"Активных загрузок: {ActiveDownloadCount}";
    }
}
