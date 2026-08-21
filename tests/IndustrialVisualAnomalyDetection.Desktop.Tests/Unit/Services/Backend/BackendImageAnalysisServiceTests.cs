using System.Net;
using System.Net.Http.Headers;
using System.Text;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Services.Backend;

public sealed class BackendImageAnalysisServiceTests
{
    private const string HeatmapBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void NullHttpClientIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new BackendImageAnalysisService(null!));
    }

    [Fact]
    public async Task SuccessfulResponseIsMappedToAnalysisResult()
    {
        using TemporaryImageFile imageFile = new(".png");
        StubHttpMessageHandler handler = new(CreateSuccessResponse());
        BackendImageAnalysisService service = CreateService(handler);

        ImageAnalysisResult result = await service.AnalyzeAsync(imageFile.Path);

        Assert.Equal("mvtec-ad-capsule-320", result.ModelId);
        Assert.Equal("capsule", result.Category);
        Assert.Equal(4.992109298706055, result.Score);
        Assert.Equal(2.501821517944336, result.Threshold);
        Assert.Equal(AnalysisDecision.Anomalous, result.Decision);
        Assert.Equal(1692, result.ProcessingTimeMs);
        Assert.Equal("trace-001", result.TraceId);
        Assert.Equal("image/png", result.Heatmap.ContentType);
        Assert.Equal(320, result.Heatmap.Width);
        Assert.Equal(320, result.Heatmap.Height);
        Assert.Equal(HeatmapBase64, result.Heatmap.DataBase64);
    }

    [Fact]
    public async Task ImageIsSentAsMultipartFormData()
    {
        using TemporaryImageFile imageFile = new(".png");
        StubHttpMessageHandler handler = new(CreateSuccessResponse());
        BackendImageAnalysisService service = CreateService(handler);

        await service.AnalyzeAsync(imageFile.Path);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://localhost:7056/api/v1/analyses", handler.RequestUri?.ToString());
        Assert.Equal("multipart/form-data", handler.RequestContentType?.MediaType);
        Assert.Equal("image", handler.ImageFieldName);
        Assert.Equal(System.IO.Path.GetFileName(imageFile.Path), handler.ImageFileName);
        Assert.Equal("image/png", handler.ImageContentType?.MediaType);
        Assert.Equal(TemporaryImageFile.Content, handler.ImageBytes);
        Assert.Null(handler.ModelId);
    }

    [Fact]
    public async Task SelectedModelIdIsSentAsMultipartFormData()
    {
        using TemporaryImageFile imageFile = new(".png");
        StubHttpMessageHandler handler = new(CreateSuccessResponse());
        BackendImageAnalysisService service = CreateService(handler);

        await service.AnalyzeAsync(
            imageFile.Path,
            "visa-cashew-generalized-q95-320");

        Assert.Equal(
            "visa-cashew-generalized-q95-320",
            handler.ModelId);
    }

    [Fact]
    public async Task WhitespaceModelIdIsRejected()
    {
        using TemporaryImageFile imageFile = new(".png");
        BackendImageAnalysisService service =
            CreateService(new StubHttpMessageHandler(CreateSuccessResponse()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AnalyzeAsync(imageFile.Path, " "));
    }

    [Fact]
    public async Task UnsupportedImageExtensionIsRejected()
    {
        using TemporaryImageFile imageFile = new(".gif");
        BackendImageAnalysisService service = CreateService(new StubHttpMessageHandler(CreateSuccessResponse()));

        await Assert.ThrowsAsync<NotSupportedException>(() => service.AnalyzeAsync(imageFile.Path));
    }

    [Fact]
    public async Task UnsuccessfulResponseIsRejected()
    {
        using TemporaryImageFile imageFile = new(".jpg");
        HttpResponseMessage response = new(HttpStatusCode.BadRequest);
        BackendImageAnalysisService service = CreateService(new StubHttpMessageHandler(response));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeAsync(imageFile.Path));
    }

    [Fact]
    public async Task UnsupportedDecisionIsRejected()
    {
        using TemporaryImageFile imageFile = new(".jpeg");
        HttpResponseMessage response = CreateSuccessResponse(decision: "unknown");
        BackendImageAnalysisService service = CreateService(new StubHttpMessageHandler(response));

        await Assert.ThrowsAsync<InvalidDataException>(() => service.AnalyzeAsync(imageFile.Path));
    }

    private static BackendImageAnalysisService CreateService(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://localhost:7056/")
        };

        return new BackendImageAnalysisService(httpClient);
    }

    private static HttpResponseMessage CreateSuccessResponse(string decision = "anomalous")
    {
        string json = $$"""
        {
          "model": {
            "id": "mvtec-ad-capsule-320",
            "category": "capsule"
          },
          "score": 4.992109298706055,
          "threshold": 2.501821517944336,
          "decision": "{{decision}}",
          "processingTimeMs": 1692,
          "traceId": "trace-001",
          "heatmap": {
            "contentType": "image/png",
            "width": 320,
            "height": 320,
            "dataBase64": "{{HeatmapBase64}}"
          }
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);

            _response = response;
        }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public MediaTypeHeaderValue? RequestContentType { get; private set; }

        public string? ImageFieldName { get; private set; }

        public string? ImageFileName { get; private set; }

        public MediaTypeHeaderValue? ImageContentType { get; private set; }

        public byte[]? ImageBytes { get; private set; }

        public string? ModelId { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            RequestContentType = request.Content?.Headers.ContentType;

            if (request.Content is MultipartFormDataContent multipartContent)
            {
                HttpContent? imageContent = multipartContent.FirstOrDefault(content =>
                    content.Headers.ContentDisposition?.Name?.Trim('"') == "image");

                if (imageContent is not null)
                {
                    ImageFieldName = imageContent.Headers.ContentDisposition?.Name?.Trim('"');
                    ImageFileName = imageContent.Headers.ContentDisposition?.FileName?.Trim('"');
                    ImageContentType = imageContent.Headers.ContentType;
                    ImageBytes = await imageContent.ReadAsByteArrayAsync(cancellationToken);
                }

                HttpContent? modelIdContent = multipartContent.FirstOrDefault(content =>
                    content.Headers.ContentDisposition?.Name?.Trim('"') == "modelId");

                if (modelIdContent is not null)
                {
                    ModelId = await modelIdContent.ReadAsStringAsync(cancellationToken);
                }
            }

            return _response;
        }
    }

    private sealed class TemporaryImageFile : IDisposable
    {
        public static readonly byte[] Content = [1, 2, 3, 4];

        public TemporaryImageFile(string extension)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ivad-desktop-{Guid.NewGuid():N}{extension}");

            File.WriteAllBytes(Path, Content);
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
