using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public interface IImageAnalysisService
{
    Task<ImageAnalysisResult> AnalyzeAsync(
        string imagePath,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}
