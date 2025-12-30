using Gateway.Api.Common.Constants;
using Gateway.Api.Configuration;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

namespace Gateway.Api.Services.Implementations;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase _database;

    //private readonly RateLimitOptions _options;

    //private const int MaxRequests = 5;

    //private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public RateLimitResult Evaluate(string apiKey, int maxRequests, int windowSeconds)
    {
        var redisKey = RedisKeys.RateLimit(apiKey);

        var requestCount = _database.StringIncrement(redisKey);

        if (requestCount == 1)
        {
            _database.KeyExpire(redisKey, TimeSpan.FromMinutes(windowSeconds));
        }

        var ttl = _database.KeyTimeToLive(redisKey);

        var remaining = maxRequests - (int)requestCount;

        var resetTime = ttl.HasValue
            ? DateTime.UtcNow.Add(ttl.Value)
            : DateTime.UtcNow.AddSeconds(windowSeconds);

        return new RateLimitResult
        {
            IsAllowed = requestCount <= maxRequests,
            Limit = maxRequests,
            Remaining = Math.Max(remaining, 0),
            ResetTime = resetTime,
        };
    }
}
