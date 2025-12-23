using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("proxy")]
public class ProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ProxyController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var downstreamUrl = "http://localhost:5099/products";

        var response = await _httpClient.GetAsync(downstreamUrl);

        var content = await response.Content.ReadAsStringAsync();

        return Content(
            content,
            response.Content.Headers.ContentType?.ToString() ?? "application/json"
        );
    }
}
