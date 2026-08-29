namespace Gateway.Api.Common.Errors;

public static class ErrorMessages
{
    public const string MissingApiKey = "API key is missing";
    public const string InvalidApiKey = "Invalid API key";
    public const string ApiKeyDisabled = "API key is disabled.";
    public const string RateLimitExceeded = "Rate limit exceeded. Try again later.";
    public const string RouteNotFound = "No downstream route matched the request path.";
}
