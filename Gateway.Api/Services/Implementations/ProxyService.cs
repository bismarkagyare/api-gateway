using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Services.Implementations;

public class ProxyService : IProxyService
{
    private readonly HttpClient _httpClient;

    public ProxyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ForwardAsync(HttpContext context)
    {
        var downstreamUrl = "http://localhost:5099/products";

        //create a new outbound http request
        var request = new HttpRequestMessage(HttpMethod.Get, downstreamUrl);

        var response = await _httpClient.SendAsync(request);

        context.Response.StatusCode = (int)response.StatusCode;

        //copy content from downstream to gateway response
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString();

        //read downstream response body
        var content = await response.Content.ReadAsStringAsync();

        //write the response body back to the client
        await context.Response.WriteAsync(content);
    }
}
