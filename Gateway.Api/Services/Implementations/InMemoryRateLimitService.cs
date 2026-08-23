using System.Collections.Concurrent;
using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Services.Implementations;

public class InMemoryRateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();

    public RateLimitResult Evaluate(string apiKey, int maxRequests, int windowSeconds)
    {
        var now = DateTime.UtcNow;

        var windowDuration = TimeSpan.FromSeconds(windowSeconds);

        var entry = _rateLimits.GetOrAdd(
            apiKey,
            _ => new RateLimitEntry { RequestCount = 0, WindowStart = now }
        );

        //check if the current window has expired and reset
        if (now - entry.WindowStart > windowDuration)
        {
            entry.WindowStart = now;
            entry.RequestCount = 0;
        }

        entry.RequestCount++;

        var remaining = maxRequests - entry.RequestCount;

        var resetTime = entry.WindowStart.Add(windowDuration);

        return new RateLimitResult
        {
            IsAllowed = entry.RequestCount <= maxRequests,
            Limit = maxRequests,
            Remaining = Math.Max(remaining, 0),
            ResetTime = resetTime,
        };
    }
}
