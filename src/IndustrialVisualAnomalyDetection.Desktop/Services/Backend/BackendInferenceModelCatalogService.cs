using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using IndustrialVisualAnomalyDetection.Desktop.Models.Inference;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public sealed class BackendInferenceModelCatalogService : IInferenceModelCatalogService
{
    private const string ModelCatalogPath = "api/v1/models";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public BackendInferenceModelCatalogService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public async Task<InferenceModelCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ModelCatalogPath, cancellationToken);

        response.EnsureSuccessStatusCode();

        BackendModelCatalogResponse? backendResponse =
            await response.Content.ReadFromJsonAsync<BackendModelCatalogResponse>(
                JsonOptions,
                cancellationToken);

        if (backendResponse?.Models is null)
        {
            throw new InvalidDataException(
                "The backend returned an incomplete model catalog.");
        }

        try
        {
            return new InferenceModelCatalog(
                backendResponse.DefaultModelId,
                backendResponse.Models.Select(model => new InferenceModel(
                    model.Id,
                    model.DisplayName,
                    model.Category,
                    model.InputSize,
                    model.IsDefault)));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The backend returned an invalid model catalog.",
                exception);
        }
    }

    private sealed class BackendModelCatalogResponse
    {
        public string DefaultModelId { get; init; } = string.Empty;
        public IReadOnlyList<BackendModelResponse>? Models { get; init; }
    }

    private sealed class BackendModelResponse
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int InputSize { get; init; }
        public bool IsDefault { get; init; }
    }
}
