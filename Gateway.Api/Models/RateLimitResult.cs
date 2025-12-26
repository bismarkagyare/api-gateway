namespace Gateway.Api.Models;

public class RateLimitResult
{
    public bool IsAllowed { get; set; }

    public int Limit { get; set; }

    public int Remaining { get; set; }

    public DateTime ResetTime { get; set; }
}
