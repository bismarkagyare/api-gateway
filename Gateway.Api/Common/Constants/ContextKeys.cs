namespace Gateway.Api.Common.Constants;

public static class ContextKeys
{
    // Key under which ApiKeyMetadata is stored on HttpContext.Items between
    // the authentication and rate-limiting middleware.
    public const string ApiKeyMetadata = "ApiKeyMetadata";
}
