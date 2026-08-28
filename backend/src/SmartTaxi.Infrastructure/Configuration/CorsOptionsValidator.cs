using Microsoft.Extensions.Options;

namespace SmartTaxi.Infrastructure.Configuration;

/// <summary>Each configured origin must be a bare absolute http(s) origin — no wildcard, no path/query/fragment. An empty list is valid and means the policy is disabled.</summary>
public sealed class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        var errors = new List<string>();

        foreach (var origin in options.AllowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                errors.Add("Cors:AllowedOrigins contains a blank entry.");
                continue;
            }

            if (origin.Contains('*'))
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' must not contain a wildcard.");
                continue;
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' is not an absolute URI.");
                continue;
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' must use http or https.");
            }

            if (uri.AbsolutePath != "/")
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' must not include a path.");
            }

            if (!string.IsNullOrEmpty(uri.Query))
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' must not include a query string.");
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                errors.Add($"Cors:AllowedOrigins entry '{origin}' must not include a fragment.");
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
