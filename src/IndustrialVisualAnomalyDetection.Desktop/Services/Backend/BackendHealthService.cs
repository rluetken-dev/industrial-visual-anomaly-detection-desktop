using System.Net.Http;
using System.Net.Http.Json;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public sealed class BackendHealthService : IBackendHealthService
{
    private const string LivenessPath = "health/live";
    private const string ReadinessPath = "health/ready";

    private readonly HttpClient _httpClient;

    public BackendHealthService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public Task<bool> IsLiveAsync(CancellationToken cancellationToken = default)
    {
        return HasExpectedStatusAsync(LivenessPath, "healthy", cancellationToken);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        return HasExpectedStatusAsync(ReadinessPath, "ready", cancellationToken);
    }

    private async Task<bool> HasExpectedStatusAsync(
        string requestPath,
        string expectedStatus,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(requestPath, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        HealthResponse? healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

        return string.Equals(healthResponse?.Status, expectedStatus, StringComparison.Ordinal);
    }

    private sealed record HealthResponse(string Status);
}
