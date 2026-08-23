using Gateway.Api.Common.Constants;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

namespace Gateway.Api.Services.Implementations;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase _database;

    // Atomically increments the counter and sets the expiry on the first request
    // in a single round trip, eliminating the race between INCR and EXPIRE.
    private const string RateLimitScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[1]) * 1000)
        end
        local ttlMs = redis.call('PTTL', KEYS[1])
        return { count, ttlMs }
        """;

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public RateLimitResult Evaluate(string apiKey, int maxRequests, int windowSeconds)
    {
        var redisKey = RedisKeys.RateLimit(apiKey);

        var result = _database.ScriptEvaluate(
            RateLimitScript,
            new RedisKey[] { redisKey },
            new RedisValue[] { windowSeconds }
        );

        var values = (RedisValue[]?)result ?? Array.Empty<RedisValue>();
        var requestCount = values.Length > 0 ? (long)values[0] : 0;
        var ttlMs = values.Length > 1 ? (long)values[1] : 0;

        var remaining = maxRequests - (int)requestCount;

        var resetTime = ttlMs > 0
            ? DateTime.UtcNow.AddMilliseconds(ttlMs)
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
