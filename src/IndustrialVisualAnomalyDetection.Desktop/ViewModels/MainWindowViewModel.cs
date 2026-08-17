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

namespace IndustrialVisualAnomalyDetection.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IBackendHealthService _backendHealthService;
    private readonly IImageFilePicker _imageFilePicker;
    private readonly IImagePreviewLoader _imagePreviewLoader;
    private readonly IImageAnalysisService _imageAnalysisService;
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

    public MainWindowViewModel(
        IBackendHealthService backendHealthService,
        IImageFilePicker imageFilePicker,
        IImagePreviewLoader imagePreviewLoader,
        IImageAnalysisService imageAnalysisService)
    {
        ArgumentNullException.ThrowIfNull(backendHealthService);
        ArgumentNullException.ThrowIfNull(imageFilePicker);
        ArgumentNullException.ThrowIfNull(imagePreviewLoader);
        ArgumentNullException.ThrowIfNull(imageAnalysisService);

        _backendHealthService = backendHealthService;
        _imageFilePicker = imageFilePicker;
        _imagePreviewLoader = imagePreviewLoader;
        _imageAnalysisService = imageAnalysisService;
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
        return _selectedImagePath is not null && !IsAnalysisRunning;
    }

    [RelayCommand(CanExecute = nameof(CanAnalyzeImage), IncludeCancelCommand = true)]
    private async Task AnalyzeImageAsync(CancellationToken cancellationToken)
    {
        if (_selectedImagePath is null)
        {
            return;
        }

        IsAnalysisRunning = true;
        ResetAnalysisResult();
        StatusText = "Analyzing the selected image...";

        try
        {
            ImageAnalysisResult result = await _imageAnalysisService.AnalyzeAsync(
                _selectedImagePath,
                cancellationToken);

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
    }
}
