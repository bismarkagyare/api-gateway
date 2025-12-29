namespace Gateway.Api.Configuration;

public class RateLimitOptions
{
    public int MaxRequests { get; set; }

    public int WindowSeconds { get; set; }
}
