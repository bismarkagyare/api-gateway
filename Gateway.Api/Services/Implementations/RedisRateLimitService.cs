using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

namespace Gateway.Api.Services.Implementations;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase _database;

    private const int MaxRequests = 5;

    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public RateLimitResult Evaluate(string apiKey)
    {
        var redisKey = $"ratelimit:{apiKey}";

        var requestCount = _database.StringIncrement(redisKey);

        if (requestCount == 1)
        {
            _database.KeyExpire(redisKey, WindowDuration);
        }

        var ttl = _database.KeyTimeToLive(redisKey);

        var remaining = MaxRequests - (int)requestCount;

        var resetTime = ttl.HasValue
            ? DateTime.UtcNow.Add(ttl.Value)
            : DateTime.UtcNow.Add(WindowDuration);

        return new RateLimitResult
        {
            IsAllowed = requestCount <= MaxRequests,
            Limit = MaxRequests,
            Remaining = Math.Max(remaining, 0),
            ResetTime = resetTime,
        };
    }
}
