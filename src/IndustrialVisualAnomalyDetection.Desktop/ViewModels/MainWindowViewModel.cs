using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;
using IndustrialVisualAnomalyDetection.Desktop.Models.Status;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;
using IndustrialVisualAnomalyDetection.Desktop.Services.Files;
using IndustrialVisualAnomalyDetection.Desktop.Services.Images;
using System.Collections.ObjectModel;
using System.Text.Json;
using IndustrialVisualAnomalyDetection.Desktop.Models.Inference;

namespace IndustrialVisualAnomalyDetection.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IBackendHealthService _backendHealthService;
    private readonly IImageFilePicker _imageFilePicker;
    private readonly IImagePreviewLoader _imagePreviewLoader;
    private readonly IHeatmapImageLoader _heatmapImageLoader;
    private readonly IImageAnalysisService _imageAnalysisService;
    private readonly IInferenceModelCatalogService _modelCatalogService;
    private string? _selectedImagePath;

    [ObservableProperty]
    private string _backendLivenessText = "Backend not checked";

    [ObservableProperty]
    private string _inferenceReadinessText = "Inference not checked";

    [ObservableProperty]
    private SystemAvailabilityStatus _backendAvailabilityStatus;

    [ObservableProperty]
    private SystemAvailabilityStatus _inferenceAvailabilityStatus;

    [ObservableProperty]
    private string _selectedImagePathText = "No image selected";

    [ObservableProperty]
    private ImageSource? _selectedImagePreview;

    [ObservableProperty]
    private ImageSource? _heatmapImageSource;

    [ObservableProperty]
    private bool _isHeatmapVisible = true;

    [ObservableProperty]
    private double _heatmapOpacity = 0.40;

    [ObservableProperty]
    private string _decisionText = "Awaiting analysis";

    [ObservableProperty]
    private AnalysisDecision? _currentDecision;

    [ObservableProperty]
    private string _scoreText = "—";

    [ObservableProperty]
    private string _thresholdText = "—";

    [ObservableProperty]
    private string _modelIdText = "—";

    [ObservableProperty]
    private string _categoryText = "—";

    [ObservableProperty]
    private string _processingTimeText = "—";

    [ObservableProperty]
    private string _traceIdText = "—";

    [ObservableProperty]
    private string _statusText = "Select an image to begin.";

    [ObservableProperty]
    private bool _isHealthCheckRunning;

    [ObservableProperty]
    private bool _isAnalysisRunning;

    public ObservableCollection<InferenceModel> AvailableModels { get; } = [];

    [ObservableProperty]
    private InferenceModel? _selectedModel;

    [ObservableProperty]
    private bool _isModelCatalogLoading;

    [ObservableProperty]
    private string _modelCatalogStatusText = "Models not loaded";

    public MainWindowViewModel(
        IBackendHealthService backendHealthService,
        IInferenceModelCatalogService modelCatalogService,
        IImageFilePicker imageFilePicker,
        IImagePreviewLoader imagePreviewLoader,
        IHeatmapImageLoader heatmapImageLoader,
        IImageAnalysisService imageAnalysisService)
    {
        ArgumentNullException.ThrowIfNull(backendHealthService);
        ArgumentNullException.ThrowIfNull(modelCatalogService);
        ArgumentNullException.ThrowIfNull(imageFilePicker);
        ArgumentNullException.ThrowIfNull(imagePreviewLoader);
        ArgumentNullException.ThrowIfNull(heatmapImageLoader);
        ArgumentNullException.ThrowIfNull(imageAnalysisService);

        _backendHealthService = backendHealthService;
        _modelCatalogService = modelCatalogService;
        _imageFilePicker = imageFilePicker;
        _imagePreviewLoader = imagePreviewLoader;
        _heatmapImageLoader = heatmapImageLoader;
        _imageAnalysisService = imageAnalysisService;
    }

    private bool CanRefreshModelCatalog()
    {
        return !IsModelCatalogLoading && !IsAnalysisRunning;
    }

    [RelayCommand(CanExecute = nameof(CanRefreshModelCatalog))]
    private async Task RefreshModelCatalogAsync(CancellationToken cancellationToken)
    {
        IsModelCatalogLoading = true;
        ModelCatalogStatusText = "Loading available models...";

        try
        {
            InferenceModelCatalog catalog =
                await _modelCatalogService.GetCatalogAsync(cancellationToken);

            AvailableModels.Clear();

            foreach (InferenceModel model in catalog.Models)
            {
                AvailableModels.Add(model);
            }

            SelectedModel = AvailableModels.Single(model =>
                model.Id == catalog.DefaultModelId);

            ModelCatalogStatusText =
                $"{AvailableModels.Count} model(s) available";
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            ClearModelCatalog();
            ModelCatalogStatusText = "Loading models was cancelled";
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or InvalidDataException
            or JsonException)
        {
            ClearModelCatalog();
            ModelCatalogStatusText = "Model catalog unavailable";
        }
        finally
        {
            IsModelCatalogLoading = false;
        }
    }

    private void ClearModelCatalog()
    {
        AvailableModels.Clear();
        SelectedModel = null;
    }

    private bool CanSelectImage()
    {
        return !IsAnalysisRunning;
    }

    [RelayCommand(CanExecute = nameof(CanSelectImage))]
    private void SelectImage()
    {
        string? selectedImagePath = _imageFilePicker.SelectImage();

        if (selectedImagePath is null)
        {
            return;
        }

        try
        {
            SelectedImagePreview = _imagePreviewLoader.Load(selectedImagePath);
        }
        catch (Exception exception) when (
            exception is IOException
            or NotSupportedException
            or UriFormatException)
        {
            _selectedImagePath = null;
            SelectedImagePathText = selectedImagePath;
            SelectedImagePreview = null;
            StatusText = "The selected image could not be loaded.";
            AnalyzeImageCommand.NotifyCanExecuteChanged();
            return;
        }

        _selectedImagePath = selectedImagePath;
        SelectedImagePathText = selectedImagePath;
        ResetAnalysisResult();
        StatusText = "Image selected and ready for analysis.";
        AnalyzeImageCommand.NotifyCanExecuteChanged();
    }

    private bool CanAnalyzeImage()
    {
        return _selectedImagePath is not null
            && SelectedModel is not null
            && !IsAnalysisRunning;
    }

    [RelayCommand(CanExecute = nameof(CanAnalyzeImage), IncludeCancelCommand = true)]
    private async Task AnalyzeImageAsync(CancellationToken cancellationToken)
    {
        if (_selectedImagePath is null || SelectedModel is null)
        {
            return;
        }

        InferenceModel selectedModel = SelectedModel;

        IsAnalysisRunning = true;
        ResetAnalysisResult();
        StatusText = "Analyzing the selected image...";

        try
        {
            ImageAnalysisResult result = await _imageAnalysisService.AnalyzeAsync(
                _selectedImagePath,
                selectedModel.Id,
                cancellationToken);

            HeatmapImageSource = _heatmapImageLoader.Load(result.Heatmap);
            DecisionText = result.Decision == AnalysisDecision.Anomalous ? "Anomalous" : "Normal";
            CurrentDecision = result.Decision;
            ScoreText = result.Score.ToString("F6", CultureInfo.InvariantCulture);
            ThresholdText = result.Threshold.ToString("F6", CultureInfo.InvariantCulture);
            ModelIdText = result.ModelId;
            CategoryText = result.Category;
            ProcessingTimeText = $"{result.ProcessingTimeMs} ms";
            TraceIdText = result.TraceId;
            StatusText = "Analysis completed successfully.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "The analysis was cancelled.";
        }
        catch (HttpRequestException)
        {
            StatusText = "The backend could not complete the analysis.";
        }
        catch (Exception exception) when (
            exception is IOException
            or InvalidDataException
            or NotSupportedException)
        {
            StatusText = "The image or backend response could not be processed.";
        }
        finally
        {
            IsAnalysisRunning = false;
        }
    }

    private void ResetAnalysisResult()
    {
        DecisionText = "Awaiting analysis";
        CurrentDecision = null;
        ScoreText = "—";
        ThresholdText = "—";
        ModelIdText = "—";
        CategoryText = "—";
        ProcessingTimeText = "—";
        TraceIdText = "—";
        HeatmapImageSource = null;
    }

    private bool CanRefreshHealth()
    {
        return !IsHealthCheckRunning;
    }

    [RelayCommand(CanExecute = nameof(CanRefreshHealth))]
    private async Task RefreshHealthAsync(CancellationToken cancellationToken)
    {
        IsHealthCheckRunning = true;
        BackendLivenessText = "Checking backend...";
        InferenceReadinessText = "Waiting for backend";
        BackendAvailabilityStatus = SystemAvailabilityStatus.Checking;
        InferenceAvailabilityStatus = SystemAvailabilityStatus.Unknown;
        StatusText = "Checking system availability...";

        try
        {
            bool isLive = await _backendHealthService.IsLiveAsync(cancellationToken);

            if (!isLive)
            {
                BackendLivenessText = "Backend unavailable";
                InferenceReadinessText = "Inference unavailable";
                BackendAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
                InferenceAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
                StatusText = "The backend did not report a healthy status.";
                return;
            }

            BackendLivenessText = "Backend healthy";
            BackendAvailabilityStatus = SystemAvailabilityStatus.Available;

            bool isReady = await _backendHealthService.IsReadyAsync(cancellationToken);
            InferenceReadinessText = isReady ? "Inference ready" : "Inference not ready";
            InferenceAvailabilityStatus = isReady
                ? SystemAvailabilityStatus.Available
                : SystemAvailabilityStatus.Unavailable;
            StatusText = isReady
                ? "The system is ready for image analysis."
                : "The backend is live, but inference is not ready.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            BackendLivenessText = "Health check cancelled";
            InferenceReadinessText = "Inference not checked";
            BackendAvailabilityStatus = SystemAvailabilityStatus.Unknown;
            InferenceAvailabilityStatus = SystemAvailabilityStatus.Unknown;
            StatusText = "The health check was cancelled.";
        }
        catch (TaskCanceledException)
        {
            BackendLivenessText = "Backend timeout";
            InferenceReadinessText = "Inference unavailable";
            BackendAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
            InferenceAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
            StatusText = "The backend health request timed out.";
        }
        catch (HttpRequestException)
        {
            BackendLivenessText = "Backend unreachable";
            InferenceReadinessText = "Inference unavailable";
            BackendAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
            InferenceAvailabilityStatus = SystemAvailabilityStatus.Unavailable;
            StatusText = "Could not connect to the backend.";
        }
        finally
        {
            IsHealthCheckRunning = false;
        }
    }

    partial void OnIsHealthCheckRunningChanged(bool value)
    {
        RefreshHealthCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsAnalysisRunningChanged(bool value)
    {
        SelectImageCommand.NotifyCanExecuteChanged();
        AnalyzeImageCommand.NotifyCanExecuteChanged();
        RefreshModelCatalogCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedModelChanged(InferenceModel? value)
    {
        AnalyzeImageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsModelCatalogLoadingChanged(bool value)
    {
        RefreshModelCatalogCommand.NotifyCanExecuteChanged();
        AnalyzeImageCommand.NotifyCanExecuteChanged();
    }
}
