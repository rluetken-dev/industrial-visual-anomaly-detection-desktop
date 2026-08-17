using System.Windows;
using IndustrialVisualAnomalyDetection.Desktop.ViewModels;

namespace IndustrialVisualAnomalyDetection.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
