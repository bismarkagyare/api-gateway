using Gateway.Api.Configuration;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

namespace Gateway.Api.Services.Implementations;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase _database;

    private readonly RateLimitOptions _options;

    //private const int MaxRequests = 5;

    //private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    public RedisRateLimitService(IConnectionMultiplexer redis, RateLimitOptions options)
    {
        _database = redis.GetDatabase();
        _options = options;
    }

    public RateLimitResult Evaluate(string apiKey)
    {
        var redisKey = $"ratelimit:{apiKey}";

        var requestCount = _database.StringIncrement(redisKey);

        if (requestCount == 1)
        {
            _database.KeyExpire(redisKey, TimeSpan.FromMinutes(_options.WindowSeconds));
        }

        var ttl = _database.KeyTimeToLive(redisKey);

        var remaining = _options.MaxRequests - (int)requestCount;

        var resetTime = ttl.HasValue
            ? DateTime.UtcNow.Add(ttl.Value)
            : DateTime.UtcNow.AddSeconds(_options.WindowSeconds);

        return new RateLimitResult
        {
            IsAllowed = requestCount <= _options.MaxRequests,
            Limit = _options.MaxRequests,
            Remaining = Math.Max(remaining, 0),
            ResetTime = resetTime,
        };
    }
}
