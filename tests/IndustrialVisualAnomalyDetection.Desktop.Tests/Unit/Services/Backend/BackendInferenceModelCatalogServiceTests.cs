using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using IndustrialVisualAnomalyDetection.Desktop.Models.Inference;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Services.Backend;

public sealed class BackendInferenceModelCatalogServiceTests
{
    [Fact]
    public void NullHttpClientIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BackendInferenceModelCatalogService(null!));
    }

    [Fact]
    public async Task SuccessfulResponseIsMappedToModelCatalog()
    {
        BackendInferenceModelCatalogService service = CreateService(
            HttpStatusCode.OK,
            """
            {
              "defaultModelId": "capsule",
              "models": [
                {
                  "id": "capsule",
                  "displayName": "MVTec AD - Capsule",
                  "category": "capsule",
                  "inputSize": 320,
                  "isDefault": true
                },
                {
                  "id": "cashew",
                  "displayName": "VisA - Cashew",
                  "category": "cashew",
                  "inputSize": 320,
                  "isDefault": false
                }
              ]
            }
            """);

        InferenceModelCatalog catalog = await service.GetCatalogAsync();

        Assert.Equal("capsule", catalog.DefaultModelId);
        Assert.Collection(
            catalog.Models,
            model =>
            {
                Assert.Equal("capsule", model.Id);
                Assert.Equal("MVTec AD - Capsule", model.DisplayName);
                Assert.Equal("capsule", model.Category);
                Assert.Equal(320, model.InputSize);
                Assert.True(model.IsDefault);
            },
            model =>
            {
                Assert.Equal("cashew", model.Id);
                Assert.Equal("VisA - Cashew", model.DisplayName);
                Assert.Equal("cashew", model.Category);
                Assert.Equal(320, model.InputSize);
                Assert.False(model.IsDefault);
            });
    }

    [Fact]
    public async Task UnsuccessfulResponseThrowsHttpRequestException()
    {
        BackendInferenceModelCatalogService service = CreateService(
            HttpStatusCode.ServiceUnavailable,
            """{"status":503}""");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetCatalogAsync());
    }

    [Fact]
    public async Task MissingModelsThrowsInvalidDataException()
    {
        BackendInferenceModelCatalogService service = CreateService(
            HttpStatusCode.OK,
            """
            {
              "defaultModelId": "capsule"
            }
            """);

        InvalidDataException exception =
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.GetCatalogAsync());

        Assert.Equal(
            "The backend returned an incomplete model catalog.",
            exception.Message);
    }

    [Fact]
    public async Task InvalidModelThrowsInvalidDataException()
    {
        BackendInferenceModelCatalogService service = CreateService(
            HttpStatusCode.OK,
            """
            {
              "defaultModelId": "capsule",
              "models": [
                {
                  "id": "capsule",
                  "displayName": "MVTec AD - Capsule",
                  "category": "capsule",
                  "inputSize": 0,
                  "isDefault": true
                }
              ]
            }
            """);

        InvalidDataException exception =
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.GetCatalogAsync());

        Assert.Equal(
            "The backend returned an invalid model catalog.",
            exception.Message);
        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public async Task MalformedJsonThrowsJsonException()
    {
        BackendInferenceModelCatalogService service = CreateService(
            HttpStatusCode.OK,
            "{");

        await Assert.ThrowsAsync<JsonException>(() =>
            service.GetCatalogAsync());
    }

    private static BackendInferenceModelCatalogService CreateService(
        HttpStatusCode statusCode,
        string responseContent)
    {
        StubHttpMessageHandler handler = new(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/models", request.RequestUri?.AbsolutePath);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    responseContent,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://localhost:7056")
        };

        return new BackendInferenceModelCatalogService(httpClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
