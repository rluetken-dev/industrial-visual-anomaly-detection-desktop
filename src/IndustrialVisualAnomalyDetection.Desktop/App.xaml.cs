using System.Windows;
using IndustrialVisualAnomalyDetection.Desktop.Configuration;
using IndustrialVisualAnomalyDetection.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using IndustrialVisualAnomalyDetection.Desktop.ViewModels;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;
using IndustrialVisualAnomalyDetection.Desktop.Services.Files;
using IndustrialVisualAnomalyDetection.Desktop.Services.Images;

namespace IndustrialVisualAnomalyDetection.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        HostApplicationBuilderSettings settings = new()
        {
            ContentRootPath = AppContext.BaseDirectory
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

        // Configuration and validation
        builder.Services.AddSingleton<IValidateOptions<BackendOptions>, BackendOptionsValidator>();

        builder.Services.AddOptions<BackendOptions>()
            .Bind(builder.Configuration.GetSection(BackendOptions.SectionName))
            .ValidateOnStart();

        // Backend communication
        builder.Services.AddHttpClient<IBackendHealthService, BackendHealthService>(
            (serviceProvider, httpClient) =>
            {
                BackendOptions options = serviceProvider.GetRequiredService<IOptions<BackendOptions>>().Value;
                string normalizedBaseAddress = $"{options.BaseAddress.TrimEnd('/')}/";

                httpClient.BaseAddress = new Uri(normalizedBaseAddress, UriKind.Absolute);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        builder.Services.AddHttpClient<IImageAnalysisService, BackendImageAnalysisService>(
            (serviceProvider, httpClient) =>
            {
                BackendOptions options = serviceProvider.GetRequiredService<IOptions<BackendOptions>>().Value;
                string normalizedBaseAddress = $"{options.BaseAddress.TrimEnd('/')}/";

                httpClient.BaseAddress = new Uri(normalizedBaseAddress, UriKind.Absolute);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        // Desktop services
        builder.Services.AddSingleton<IImageFilePicker, ImageFilePicker>();
        builder.Services.AddSingleton<IImagePreviewLoader, ImagePreviewLoader>();
        builder.Services.AddSingleton<IHeatmapImageLoader, HeatmapImageLoader>();

        // Presentation
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host.Start();

        MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindowViewModel viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

        MainWindow = mainWindow;
        mainWindow.Show();

        await viewModel.RefreshHealthCommand.ExecuteAsync(null);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();

        base.OnExit(e);
    }
}
