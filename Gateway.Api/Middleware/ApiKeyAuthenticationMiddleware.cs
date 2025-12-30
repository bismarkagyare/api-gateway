using Gateway.Api.Common.Constants;
using Gateway.Api.Common.Errors;
using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    private readonly IApiKeyService _apiKeyService;

    //private const string validApiKey = "test-api-key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IApiKeyService apiKeyService)
    {
        _next = next;
        _apiKeyService = apiKeyService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderNames.ApiKey, out var apikeyValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(ErrorMessages.MissingApiKey);
            return;
        }

        var apiKey = apikeyValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(ErrorMessages.InvalidApiKey);
            return;
        }

        var metadata = await _apiKeyService.GetApiKeyMetadataAsync(apiKey);

        if (metadata is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(ErrorMessages.InvalidApiKey);
            return;
        }

        if (!metadata.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(ErrorMessages.ApiKeyDisabled);
            return;
        }

        context.Items["ApiKeyMetadata"] = metadata;

        await _next(context);
    }
}
