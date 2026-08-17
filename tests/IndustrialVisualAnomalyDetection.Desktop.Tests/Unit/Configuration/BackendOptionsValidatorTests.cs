using IndustrialVisualAnomalyDetection.Desktop.Configuration;
using Microsoft.Extensions.Options;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Configuration;

public sealed class BackendOptionsValidatorTests
{
    private readonly BackendOptionsValidator _validator = new();

    [Fact]
    public void ValidOptionsPassValidation()
    {
        BackendOptions options = new()
        {
            BaseAddress = "https://localhost:7056",
            TimeoutSeconds = 30
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RelativeBaseAddressFailsValidation()
    {
        BackendOptions options = new()
        {
            BaseAddress = "/api",
            TimeoutSeconds = 30
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("absolute URI", result.FailureMessage);
    }

    [Fact]
    public void UnsupportedSchemeFailsValidation()
    {
        BackendOptions options = new()
        {
            BaseAddress = "ftp://localhost",
            TimeoutSeconds = 30
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("HTTP or HTTPS", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveTimeoutFailsValidation(int timeoutSeconds)
    {
        BackendOptions options = new()
        {
            BaseAddress = "https://localhost:7056",
            TimeoutSeconds = timeoutSeconds
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("greater than zero", result.FailureMessage);
    }

    [Fact]
    public void ExcessiveTimeoutFailsValidation()
    {
        BackendOptions options = new()
        {
            BaseAddress = "https://localhost:7056",
            TimeoutSeconds = 301
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("must not exceed 300", result.FailureMessage);
    }

    [Fact]
    public void NullOptionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, null!));
    }
}
