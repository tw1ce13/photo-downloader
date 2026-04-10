using System.Windows;
using PhotoDownloader.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PhotoDownloader.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => ApplicationThemeManager.Apply(this);
    }
}
