using System.Net.Http;
using Gateway.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("proxy")]
public class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;

    public ProxyController(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    [HttpGet("products")]
    public async Task Forward()
    {
        await _proxyService.ForwardAsync(HttpContext);
    }
}
