namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public interface IBackendHealthService
{
    Task<bool> IsLiveAsync(CancellationToken cancellationToken = default);
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}
