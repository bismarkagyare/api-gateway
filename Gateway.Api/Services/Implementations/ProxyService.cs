using Gateway.Api.Common.Constants;
using Gateway.Api.Common.Errors;
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

    public async Task ForwardAsync(HttpContext context, string path)
    {
        var route = ResolveRoute(path);

        if (route is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync(ErrorMessages.RouteNotFound);
            return;
        }

        // Build the downstream URI from the route target, appending the incoming
        // path (after /proxy) and query string.
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = BuildDownstreamUri(route, path, context.Request.QueryString),
            Content = CreateContent(context.Request),
        };

        // Forward relevant request headers.
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

    private DownstreamRoute? ResolveRoute(string path)
    {
        // Match the longest configured route whose path is a prefix boundary of
        // the incoming path (e.g. "products" matches "products/123", not "productx").
        return _options.Routes
            .Where(route =>
                path.Equals(route.Path, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(route.Path + "/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(route => route.Path.Length)
            .FirstOrDefault();
    }

    private static Uri BuildDownstreamUri(DownstreamRoute route, string path, QueryString queryString)
    {
        var uriBuilder = new UriBuilder(route.Target);

        var basePath = uriBuilder.Path.TrimEnd('/');
        var forwardedPath = "/" + path.TrimStart('/');
        uriBuilder.Path = basePath + forwardedPath;

        if (queryString.HasValue)
        {
            var incomingQuery = queryString.Value.TrimStart('?');
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? incomingQuery
                : $"{uriBuilder.Query.TrimStart('?')}&{incomingQuery}";
        }

        return uriBuilder.Uri;
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
