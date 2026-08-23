using Gateway.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Gateway.Api.Services.Implementations;

public class DownstreamHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    private readonly string _downstreamUrl;

    public DownstreamHealthCheck(HttpClient httpClient, IOptions<DownstreamOptions> options)
    {
        _httpClient = httpClient;
        _downstreamUrl = options.Value.ProductsServiceUrl;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_downstreamUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Downstream responded {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Downstream responded {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Downstream is unreachable", ex);
        }
    }
}
