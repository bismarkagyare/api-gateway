using Gateway.Api.Common.Constants;
using Gateway.Api.Common.Errors;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly IRateLimitService _rateLimitService;

    public RateLimitingMiddleware(RequestDelegate next, IRateLimitService rateLimitService)
    {
        _next = next;
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (
            !context.Items.TryGetValue("ApiKeyMetadata", out var metadataObj)
            || metadataObj is not ApiKeyMetadata metadata
        )
        {
            await _next(context);
            return;
        }

        var apiKey = metadata.ApiKey;

        var result = _rateLimitService.Evaluate(
            apiKey,
            metadata.MaxRequests,
            metadata.WindowSeconds
        );

        //add rate limit headers to response
        context.Response.Headers[HeaderNames.RateLimitLimit] = result.Limit.ToString();
        context.Response.Headers[HeaderNames.RateLimitRemaining] = result.Remaining.ToString();
        context.Response.Headers[HeaderNames.RateLimitReset] = new DateTimeOffset(result.ResetTime)
            .ToUnixTimeSeconds()
            .ToString();

        //if not allowed, block the request
        if (!result.IsAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync(ErrorMessages.RateLimitExceeded);
            return;
        }

        await _next(context);
    }
}
