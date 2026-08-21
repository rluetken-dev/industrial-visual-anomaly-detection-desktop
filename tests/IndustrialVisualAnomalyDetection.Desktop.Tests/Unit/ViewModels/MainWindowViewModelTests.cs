using System.Windows.Media;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;
using IndustrialVisualAnomalyDetection.Desktop.Services.Files;
using IndustrialVisualAnomalyDetection.Desktop.Services.Images;
using IndustrialVisualAnomalyDetection.Desktop.ViewModels;
using IndustrialVisualAnomalyDetection.Desktop.Models.Inference;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void InitialStateDescribesAnIdleApplication()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        Assert.Equal("Backend not checked", viewModel.BackendLivenessText);
        Assert.Equal("Inference not checked", viewModel.InferenceReadinessText);
        Assert.Equal("No image selected", viewModel.SelectedImagePathText);
        Assert.Null(viewModel.SelectedImagePreview);
        Assert.Null(viewModel.HeatmapImageSource);
        Assert.True(viewModel.IsHeatmapVisible);
        Assert.Equal(0.40, viewModel.HeatmapOpacity);
        Assert.Equal("Awaiting analysis", viewModel.DecisionText);
        Assert.Null(viewModel.CurrentDecision);
        Assert.Equal("—", viewModel.ScoreText);
        Assert.Equal("—", viewModel.ThresholdText);
        Assert.Equal("Select an image to begin.", viewModel.StatusText);
        Assert.False(viewModel.IsHealthCheckRunning);
        Assert.False(viewModel.IsAnalysisRunning);
        Assert.Empty(viewModel.AvailableModels);
        Assert.Null(viewModel.SelectedModel);
        Assert.False(viewModel.IsModelCatalogLoading);
        Assert.Equal("Models not loaded", viewModel.ModelCatalogStatusText);
    }

    [Fact]
    public async Task ModelCatalogSelectsConfiguredDefaultModel()
    {
        InferenceModelCatalog catalog = new(
            "cashew",
            [
                new InferenceModel(
                "capsule",
                "MVTec AD - Capsule",
                "capsule",
                320,
                false),
            new InferenceModel(
                "cashew",
                "VisA - Cashew",
                "cashew",
                320,
                true)
            ]);

        StubInferenceModelCatalogService modelCatalogService = new(catalog);

        MainWindowViewModel viewModel = CreateViewModel(
            modelCatalogService: modelCatalogService);

        await viewModel.RefreshModelCatalogCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.AvailableModels.Count);
        Assert.Equal("cashew", viewModel.SelectedModel?.Id);
        Assert.Equal("2 model(s) available", viewModel.ModelCatalogStatusText);
        Assert.Equal(1, modelCatalogService.CallCount);
        Assert.False(viewModel.IsModelCatalogLoading);
    }

    [Fact]
    public async Task ModelCatalogFailureProducesUnderstandableState()
    {
        StubInferenceModelCatalogService modelCatalogService = new(
            exception: new HttpRequestException("Unavailable"));

        MainWindowViewModel viewModel = CreateViewModel(
            modelCatalogService: modelCatalogService);

        await viewModel.RefreshModelCatalogCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.AvailableModels);
        Assert.Null(viewModel.SelectedModel);
        Assert.Equal(
            "Model catalog unavailable",
            viewModel.ModelCatalogStatusText);
        Assert.False(viewModel.IsModelCatalogLoading);
    }

    [Fact]
    public void NullHealthServiceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            null!,
            new StubInferenceModelCatalogService(),
            new StubImageFilePicker(),
            new StubImagePreviewLoader(),
            new StubHeatmapImageLoader(),
            new StubImageAnalysisService()));
    }

    [Fact]
    public void NullModelCatalogServiceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new StubBackendHealthService(),
            null!,
            new StubImageFilePicker(),
            new StubImagePreviewLoader(),
            new StubHeatmapImageLoader(),
            new StubImageAnalysisService()));
    }

    [Fact]
    public void NullImageFilePickerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new StubBackendHealthService(),
            new StubInferenceModelCatalogService(),
            null!,
            new StubImagePreviewLoader(),
            new StubHeatmapImageLoader(),
            new StubImageAnalysisService()));
    }

    [Fact]
    public void NullImagePreviewLoaderIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new StubBackendHealthService(),
            new StubInferenceModelCatalogService(),
            new StubImageFilePicker(),
            null!,
            new StubHeatmapImageLoader(),
            new StubImageAnalysisService()));
    }

    [Fact]
    public void NullHeatmapImageLoaderIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new StubBackendHealthService(),
            new StubInferenceModelCatalogService(),
            new StubImageFilePicker(),
            new StubImagePreviewLoader(),
            null!,
            new StubImageAnalysisService()));
    }

    [Fact]
    public void NullImageAnalysisServiceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new StubBackendHealthService(),
            new StubInferenceModelCatalogService(),
            new StubImageFilePicker(),
            new StubImagePreviewLoader(),
            new StubHeatmapImageLoader(),
            null!));
    }

    [Fact]
    public void ChangingStatusRaisesPropertyChanged()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (_, eventArguments) =>
            changedPropertyName = eventArguments.PropertyName;

        viewModel.StatusText = "Updated status";

        Assert.Equal(nameof(MainWindowViewModel.StatusText), changedPropertyName);
    }

    [Fact]
    public void SelectedImageUpdatesPathPreviewAndStatus()
    {
        StubImageFilePicker imageFilePicker = new(@"C:\images\capsule.png");
        StubImagePreviewLoader imagePreviewLoader = new();

        MainWindowViewModel viewModel = CreateViewModel(
            imageFilePicker: imageFilePicker,
            imagePreviewLoader: imagePreviewLoader);

        viewModel.SelectImageCommand.Execute(null);

        Assert.Equal(@"C:\images\capsule.png", viewModel.SelectedImagePathText);
        Assert.Same(imagePreviewLoader.Preview, viewModel.SelectedImagePreview);
        Assert.Equal("Image selected and ready for analysis.", viewModel.StatusText);
        Assert.Equal(1, imageFilePicker.CallCount);
        Assert.Equal(@"C:\images\capsule.png", imagePreviewLoader.LoadedPath);
    }

    [Fact]
    public void CancelledImageSelectionPreservesState()
    {
        StubImagePreviewLoader imagePreviewLoader = new();

        MainWindowViewModel viewModel = CreateViewModel(
            imageFilePicker: new StubImageFilePicker(),
            imagePreviewLoader: imagePreviewLoader);

        viewModel.SelectImageCommand.Execute(null);

        Assert.Equal("No image selected", viewModel.SelectedImagePathText);
        Assert.Null(viewModel.SelectedImagePreview);
        Assert.Null(viewModel.HeatmapImageSource);
        Assert.Null(imagePreviewLoader.LoadedPath);
        Assert.Equal("Select an image to begin.", viewModel.StatusText);
    }

    [Fact]
    public void UnreadableImageProducesUnderstandableStatus()
    {
        MainWindowViewModel viewModel = CreateViewModel(
            imageFilePicker: new StubImageFilePicker(@"C:\images\invalid.png"),
            imagePreviewLoader: new StubImagePreviewLoader(new IOException("Invalid image")));

        viewModel.SelectImageCommand.Execute(null);

        Assert.Equal(@"C:\images\invalid.png", viewModel.SelectedImagePathText);
        Assert.Null(viewModel.SelectedImagePreview);
        Assert.Null(viewModel.HeatmapImageSource);
        Assert.Equal("The selected image could not be loaded.", viewModel.StatusText);
        Assert.False(viewModel.AnalyzeImageCommand.CanExecute(null));
    }

    [Fact]
    public async Task SelectedImageCanBeAnalyzed()
    {
        AnalysisHeatmap heatmap = CreateHeatmap();

        ImageAnalysisResult result = new(
            "mvtec-ad-capsule-320",
            "capsule",
            4.992109,
            2.501822,
            AnalysisDecision.Anomalous,
            1692,
            "trace-001",
            heatmap);

        StubImageAnalysisService analysisService = new(result);
        StubHeatmapImageLoader heatmapImageLoader = new();

        MainWindowViewModel viewModel = CreateViewModel(
            imageFilePicker: new StubImageFilePicker(@"C:\images\capsule.png"),
            heatmapImageLoader: heatmapImageLoader,
            imageAnalysisService: analysisService);

        await viewModel.RefreshModelCatalogCommand.ExecuteAsync(null);
        viewModel.SelectImageCommand.Execute(null);
        await viewModel.AnalyzeImageCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\images\capsule.png", analysisService.AnalyzedPath);
        Assert.Equal("Anomalous", viewModel.DecisionText);
        Assert.Equal(AnalysisDecision.Anomalous, viewModel.CurrentDecision);
        Assert.Equal("4.992109", viewModel.ScoreText);
        Assert.Equal("2.501822", viewModel.ThresholdText);
        Assert.Equal("mvtec-ad-capsule-320", viewModel.ModelIdText);
        Assert.Equal("capsule", viewModel.CategoryText);
        Assert.Equal("1692 ms", viewModel.ProcessingTimeText);
        Assert.Equal("trace-001", viewModel.TraceIdText);
        Assert.Same(heatmap, heatmapImageLoader.LoadedHeatmap);
        Assert.Same(heatmapImageLoader.ImageSource, viewModel.HeatmapImageSource);
        Assert.Equal("Analysis completed successfully.", viewModel.StatusText);
        Assert.False(viewModel.IsAnalysisRunning);
        Assert.Equal("test-model", analysisService.AnalyzedModelId);
    }

    [Fact]
    public void AnalysisCannotRunWithoutSelectedImage()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.AnalyzeImageCommand.CanExecute(null));
    }

    [Fact]
    public async Task BackendFailureProducesUnderstandableAnalysisStatus()
    {
        StubImageAnalysisService analysisService = new(
            exception: new HttpRequestException("Unavailable"));

        MainWindowViewModel viewModel = CreateViewModel(
            imageFilePicker: new StubImageFilePicker(@"C:\images\capsule.png"),
            imageAnalysisService: analysisService);

        await viewModel.RefreshModelCatalogCommand.ExecuteAsync(null);
        viewModel.SelectImageCommand.Execute(null);
        await viewModel.AnalyzeImageCommand.ExecuteAsync(null);

        Assert.Null(viewModel.HeatmapImageSource);
        Assert.Equal("The backend could not complete the analysis.", viewModel.StatusText);
        Assert.False(viewModel.IsAnalysisRunning);
    }

    [Fact]
    public async Task SuccessfulHealthCheckReportsReadySystem()
    {
        StubBackendHealthService healthService = new(isLive: true, isReady: true);
        MainWindowViewModel viewModel = CreateViewModel(healthService);

        await viewModel.RefreshHealthCommand.ExecuteAsync(null);

        Assert.Equal("Backend healthy", viewModel.BackendLivenessText);
        Assert.Equal("Inference ready", viewModel.InferenceReadinessText);
        Assert.Equal("The system is ready for image analysis.", viewModel.StatusText);
        Assert.Equal(1, healthService.LivenessCallCount);
        Assert.Equal(1, healthService.ReadinessCallCount);
        Assert.False(viewModel.IsHealthCheckRunning);
    }

    [Fact]
    public async Task UnhealthyBackendSkipsReadinessCheck()
    {
        StubBackendHealthService healthService = new(isLive: false, isReady: true);
        MainWindowViewModel viewModel = CreateViewModel(healthService);

        await viewModel.RefreshHealthCommand.ExecuteAsync(null);

        Assert.Equal("Backend unavailable", viewModel.BackendLivenessText);
        Assert.Equal("Inference unavailable", viewModel.InferenceReadinessText);
        Assert.Equal(1, healthService.LivenessCallCount);
        Assert.Equal(0, healthService.ReadinessCallCount);
        Assert.False(viewModel.IsHealthCheckRunning);
    }

    [Fact]
    public async Task NotReadyInferenceIsReportedSeparately()
    {
        StubBackendHealthService healthService = new(isLive: true, isReady: false);
        MainWindowViewModel viewModel = CreateViewModel(healthService);

        await viewModel.RefreshHealthCommand.ExecuteAsync(null);

        Assert.Equal("Backend healthy", viewModel.BackendLivenessText);
        Assert.Equal("Inference not ready", viewModel.InferenceReadinessText);
        Assert.Equal("The backend is live, but inference is not ready.", viewModel.StatusText);
    }

    [Fact]
    public async Task ConnectionFailureProducesUnderstandableStatus()
    {
        StubBackendHealthService healthService = new(
            exception: new HttpRequestException("Unavailable"));

        MainWindowViewModel viewModel = CreateViewModel(healthService);

        await viewModel.RefreshHealthCommand.ExecuteAsync(null);

        Assert.Equal("Backend unreachable", viewModel.BackendLivenessText);
        Assert.Equal("Inference unavailable", viewModel.InferenceReadinessText);
        Assert.Equal("Could not connect to the backend.", viewModel.StatusText);
        Assert.False(viewModel.IsHealthCheckRunning);
    }

    private static MainWindowViewModel CreateViewModel(
        IBackendHealthService? healthService = null,
        IInferenceModelCatalogService? modelCatalogService = null,
        IImageFilePicker? imageFilePicker = null,
        IImagePreviewLoader? imagePreviewLoader = null,
        IHeatmapImageLoader? heatmapImageLoader = null,
        IImageAnalysisService? imageAnalysisService = null)
    {
        return new MainWindowViewModel(
            healthService ?? new StubBackendHealthService(),
            modelCatalogService ?? new StubInferenceModelCatalogService(),
            imageFilePicker ?? new StubImageFilePicker(),
            imagePreviewLoader ?? new StubImagePreviewLoader(),
            heatmapImageLoader ?? new StubHeatmapImageLoader(),
            imageAnalysisService ?? new StubImageAnalysisService());
    }

    private static AnalysisHeatmap CreateHeatmap()
    {
        const string transparentPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

        return new AnalysisHeatmap(
            "image/png",
            1,
            1,
            transparentPngBase64);
    }

    private sealed class StubInferenceModelCatalogService : IInferenceModelCatalogService
    {
        private readonly InferenceModelCatalog _catalog;
        private readonly Exception? _exception;

        public StubInferenceModelCatalogService(
            InferenceModelCatalog? catalog = null,
            Exception? exception = null)
        {
            _catalog = catalog ?? new InferenceModelCatalog(
                "test-model",
                [
                    new InferenceModel(
                    "test-model",
                    "Test Model",
                    "test-category",
                    320,
                    true)
                ]);

            _exception = exception;
        }

        public int CallCount { get; private set; }

        public Task<InferenceModelCatalog> GetCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (_exception is not null)
            {
                return Task.FromException<InferenceModelCatalog>(_exception);
            }

            return Task.FromResult(_catalog);
        }
    }

    private sealed class StubImageAnalysisService : IImageAnalysisService
    {
        private readonly ImageAnalysisResult _result;
        private readonly Exception? _exception;

        public StubImageAnalysisService(
            ImageAnalysisResult? result = null,
            Exception? exception = null)
        {
            _result = result ?? new ImageAnalysisResult(
                "test-model",
                "test-category",
                1.0,
                2.0,
                AnalysisDecision.Normal,
                10,
                "test-trace",
                CreateHeatmap());

            _exception = exception;
        }

        public string? AnalyzedPath { get; private set; }

        public string? AnalyzedModelId { get; private set; }

        public Task<ImageAnalysisResult> AnalyzeAsync(
            string imagePath,
            string? modelId = null,
            CancellationToken cancellationToken = default)
        {
            AnalyzedPath = imagePath;
            AnalyzedModelId = modelId;

            if (_exception is not null)
            {
                return Task.FromException<ImageAnalysisResult>(_exception);
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class StubImageFilePicker : IImageFilePicker
    {
        private readonly string? _selectedImagePath;

        public StubImageFilePicker(string? selectedImagePath = null)
        {
            _selectedImagePath = selectedImagePath;
        }

        public int CallCount { get; private set; }

        public string? SelectImage()
        {
            CallCount++;
            return _selectedImagePath;
        }
    }

    private sealed class StubImagePreviewLoader : IImagePreviewLoader
    {
        private readonly Exception? _exception;

        public StubImagePreviewLoader(Exception? exception = null)
        {
            _exception = exception;
        }

        public ImageSource Preview { get; } = new DrawingImage();

        public string? LoadedPath { get; private set; }

        public ImageSource Load(string imagePath)
        {
            LoadedPath = imagePath;

            if (_exception is not null)
            {
                throw _exception;
            }

            return Preview;
        }
    }

    private sealed class StubHeatmapImageLoader : IHeatmapImageLoader
    {
        public ImageSource ImageSource { get; } = new DrawingImage();

        public AnalysisHeatmap? LoadedHeatmap { get; private set; }

        public ImageSource Load(AnalysisHeatmap heatmap)
        {
            ArgumentNullException.ThrowIfNull(heatmap);

            LoadedHeatmap = heatmap;
            return ImageSource;
        }
    }

    private sealed class StubBackendHealthService : IBackendHealthService
    {
        private readonly bool _isLive;
        private readonly bool _isReady;
        private readonly Exception? _exception;

        public StubBackendHealthService(
            bool isLive = true,
            bool isReady = true,
            Exception? exception = null)
        {
            _isLive = isLive;
            _isReady = isReady;
            _exception = exception;
        }

        public int LivenessCallCount { get; private set; }

        public int ReadinessCallCount { get; private set; }

        public Task<bool> IsLiveAsync(CancellationToken cancellationToken = default)
        {
            LivenessCallCount++;

            if (_exception is not null)
            {
                return Task.FromException<bool>(_exception);
            }

            return Task.FromResult(_isLive);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            ReadinessCallCount++;
            return Task.FromResult(_isReady);
        }
    }
}
