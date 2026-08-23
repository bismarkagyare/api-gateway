using Gateway.Api.Configuration;
using Gateway.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Gateway.Api.Services.Implementations;

public class ProxyService : IProxyService
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
    };

    private readonly HttpClient _httpClient;

    private readonly DownstreamOptions _options;

    public ProxyService(HttpClient httpClient, IOptions<DownstreamOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task ForwardAsync(HttpContext context)
    {
        // Build the downstream URI from configuration, appending the incoming query string.
        var uriBuilder = new UriBuilder(_options.ProductsServiceUrl);
        if (context.Request.QueryString.HasValue)
        {
            var incomingQuery = context.Request.QueryString.Value.TrimStart('?');
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? incomingQuery
                : $"{uriBuilder.Query.TrimStart('?')}&{incomingQuery}";
        }

        // Forward the incoming method, body, and headers.
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = uriBuilder.Uri,
            Content = CreateContent(context.Request),
        };

        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var response = await _httpClient.SendAsync(request, context.RequestAborted);

        // Copy the downstream response status, headers, and body back to the client.
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in response.Content.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }

    private static HttpContent? CreateContent(HttpRequest request)
    {
        if (request.ContentLength is > 0)
        {
            return new StreamContent(request.Body);
        }

        return null;
    }
}
