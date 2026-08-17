using System.Net;
using System.Text;
using IndustrialVisualAnomalyDetection.Desktop.Services.Backend;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Services.Backend;

public sealed class BackendHealthServiceTests
{
    [Fact]
    public async Task HealthyLivenessResponseReturnsTrue()
    {
        using HttpClient httpClient = CreateHttpClient(
            "/health/live",
            HttpStatusCode.OK,
            """{"status":"healthy"}""");

        BackendHealthService service = new(httpClient);

        bool isLive = await service.IsLiveAsync();

        Assert.True(isLive);
    }

    [Fact]
    public async Task UnexpectedLivenessStatusReturnsFalse()
    {
        using HttpClient httpClient = CreateHttpClient(
            "/health/live",
            HttpStatusCode.OK,
            """{"status":"unknown"}""");

        BackendHealthService service = new(httpClient);

        bool isLive = await service.IsLiveAsync();

        Assert.False(isLive);
    }

    [Fact]
    public async Task ReadyResponseReturnsTrue()
    {
        using HttpClient httpClient = CreateHttpClient(
            "/health/ready",
            HttpStatusCode.OK,
            """{"status":"ready"}""");

        BackendHealthService service = new(httpClient);

        bool isReady = await service.IsReadyAsync();

        Assert.True(isReady);
    }

    [Fact]
    public async Task ServiceUnavailableReadinessResponseReturnsFalse()
    {
        using HttpClient httpClient = CreateHttpClient(
            "/health/ready",
            HttpStatusCode.ServiceUnavailable,
            """{"status":"not_ready"}""");

        BackendHealthService service = new(httpClient);

        bool isReady = await service.IsReadyAsync();

        Assert.False(isReady);
    }

    [Fact]
    public async Task MalformedSuccessfulResponseReturnsFalse()
    {
        using HttpClient httpClient = CreateHttpClient(
            "/health/live",
            HttpStatusCode.OK,
            """{"unexpected":"value"}""");

        BackendHealthService service = new(httpClient);

        bool isLive = await service.IsLiveAsync();

        Assert.False(isLive);
    }

    [Fact]
    public void NullHttpClientIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new BackendHealthService(null!));
    }

    private static HttpClient CreateHttpClient(
        string expectedPath,
        HttpStatusCode statusCode,
        string responseContent)
    {
        StubHttpMessageHandler handler = new(request =>
        {
            Assert.Equal(expectedPath, request.RequestUri?.AbsolutePath);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7056")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            ArgumentNullException.ThrowIfNull(responseFactory);

            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
