using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

public sealed class BackendImageAnalysisService : IImageAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public BackendImageAnalysisService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public async Task<ImageAnalysisResult> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        await using FileStream imageStream = new(
            imagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        using StreamContent imageContent = new(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(imagePath));

        using MultipartFormDataContent requestContent = new();
        requestContent.Add(imageContent, "image", Path.GetFileName(imagePath));

        using HttpResponseMessage response = await _httpClient.PostAsync(
            "api/v1/analyses",
            requestContent,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        BackendAnalysisResponse? backendResponse =
            await response.Content.ReadFromJsonAsync<BackendAnalysisResponse>(
                JsonOptions,
                cancellationToken);

        if (backendResponse?.Model is null || backendResponse.Heatmap is null)
        {
            throw new InvalidDataException("The backend returned an incomplete analysis response.");
        }

        AnalysisDecision decision = backendResponse.Decision.ToLowerInvariant() switch
        {
            "normal" => AnalysisDecision.Normal,
            "anomalous" => AnalysisDecision.Anomalous,
            _ => throw new InvalidDataException("The backend returned an unsupported analysis decision.")
        };

        return new ImageAnalysisResult(
            backendResponse.Model.Id,
            backendResponse.Model.Category,
            backendResponse.Score,
            backendResponse.Threshold,
            decision,
            backendResponse.ProcessingTimeMs,
            backendResponse.TraceId,
            new AnalysisHeatmap(
                backendResponse.Heatmap.ContentType,
                backendResponse.Heatmap.Width,
                backendResponse.Heatmap.Height,
                backendResponse.Heatmap.DataBase64));
    }

    private static string GetContentType(string imagePath)
    {
        return Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => throw new NotSupportedException("Only PNG and JPEG images are supported.")
        };
    }

    private sealed class BackendAnalysisResponse
    {
        public BackendAnalysisModelResponse? Model { get; init; }

        public double Score { get; init; }

        public double Threshold { get; init; }

        public string Decision { get; init; } = string.Empty;

        public long ProcessingTimeMs { get; init; }

        public string TraceId { get; init; } = string.Empty;

        public BackendAnalysisHeatmapResponse? Heatmap { get; init; }
    }

    private sealed class BackendAnalysisModelResponse
    {
        public string Id { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;
    }

    private sealed class BackendAnalysisHeatmapResponse
    {
        public string ContentType { get; init; } = string.Empty;

        public int Width { get; init; }

        public int Height { get; init; }

        public string DataBase64 { get; init; } = string.Empty;
    }
}
