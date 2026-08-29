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

    // Catch-all that captures the path after /proxy and forwards any HTTP verb.
    [Route("{**path}")]
    public async Task Forward(string path)
    {
        await _proxyService.ForwardAsync(HttpContext, path);
    }
}
