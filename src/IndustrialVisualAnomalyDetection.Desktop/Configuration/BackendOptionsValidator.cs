using Microsoft.Extensions.Options;

namespace IndustrialVisualAnomalyDetection.Desktop.Configuration;

public sealed class BackendOptionsValidator : IValidateOptions<BackendOptions>
{
    public ValidateOptionsResult Validate(string? name, BackendOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out Uri? baseAddress))
        {
            return ValidateOptionsResult.Fail("The backend base address must be an absolute URI.");
        }

        if (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("The backend base address must use HTTP or HTTPS.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("The backend timeout must be greater than zero seconds.");
        }

        if (options.TimeoutSeconds > 300)
        {
            return ValidateOptionsResult.Fail("The backend timeout must not exceed 300 seconds.");
        }

        return ValidateOptionsResult.Success;
    }
}
