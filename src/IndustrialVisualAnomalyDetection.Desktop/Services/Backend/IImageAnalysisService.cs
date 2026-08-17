using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public interface IImageAnalysisService
{
    Task<ImageAnalysisResult> AnalyzeAsync(string imagePath, CancellationToken cancellationToken = default);
}
