using Gateway.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Gateway.Api.Services.Implementations;

public class DownstreamHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    private readonly List<DownstreamRoute> _routes;

    public DownstreamHealthCheck(HttpClient httpClient, IOptions<DownstreamOptions> options)
    {
        _httpClient = httpClient;
        _routes = options.Value.Routes;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_routes.Count == 0)
        {
            return HealthCheckResult.Unhealthy("No downstream routes configured");
        }

        var failures = new List<string>();

        foreach (var route in _routes)
        {
            var healthUrl = $"{route.Target.TrimEnd('/')}/{route.Path.TrimStart('/')}";

            try
            {
                var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"{route.Path}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{route.Path}: {ex.Message}");
            }
        }

        return failures.Count == 0
            ? HealthCheckResult.Healthy($"All {_routes.Count} downstream routes reachable")
            : HealthCheckResult.Unhealthy(string.Join("; ", failures));
    }
}
