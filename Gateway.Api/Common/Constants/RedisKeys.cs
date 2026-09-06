namespace Gateway.Api.Common.Constants;

public static class RedisKeys
{
    public static string RateLimit(string apiKey) => $"ratelimit:{apiKey}";

    public static string ApiKeyMetadata(string apiKey) => $"apikey:meta:{apiKey}";
}
