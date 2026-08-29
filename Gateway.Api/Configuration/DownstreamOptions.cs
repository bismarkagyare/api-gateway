namespace Gateway.Api.Configuration;

public class DownstreamRoute
{
    public string Path { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;
}

public class DownstreamOptions
{
    public List<DownstreamRoute> Routes { get; set; } = new();
}
