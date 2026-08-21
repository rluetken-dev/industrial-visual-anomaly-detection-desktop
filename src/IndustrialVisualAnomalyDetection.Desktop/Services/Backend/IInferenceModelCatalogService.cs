using IndustrialVisualAnomalyDetection.Desktop.Models.Inference;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public interface IInferenceModelCatalogService
{
    Task<InferenceModelCatalog> GetCatalogAsync(CancellationToken cancellationToken = default);
}
