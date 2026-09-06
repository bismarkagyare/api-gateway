using Gateway.Api.Common.Constants;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

namespace Gateway.Api.Services.Implementations;

public class ApiKeyService : IApiKeyService
{
    private readonly IDatabase _redisDatabase;

    public ApiKeyService(IConnectionMultiplexer redis)
    {
        _redisDatabase = redis.GetDatabase();
    }

    public async Task<ApiKeyMetadata?> GetApiKeyMetadataAsync(string apiKey)
    {
        var redisKey = RedisKeys.ApiKeyMetadata(apiKey);

        var hashEntries = await _redisDatabase.HashGetAllAsync(redisKey);

        if (hashEntries.Length == 0)
        {
            return null;
        }

        var hash = hashEntries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString()
        );

        var metadata = new ApiKeyMetadata
        {
            ApiKey = apiKey,
            Plan = hash.GetValueOrDefault("plan", string.Empty),
            MaxRequests = int.TryParse(hash.GetValueOrDefault("maxRequests"), out var maxReq)
                ? maxReq
                : 0,
            WindowSeconds = int.TryParse(hash.GetValueOrDefault("windowSeconds"), out var window)
                ? window
                : 0,
            IsActive = bool.TryParse(hash.GetValueOrDefault("isActive"), out var active) && active,
        };

        return metadata;
    }
}
