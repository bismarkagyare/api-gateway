namespace Gateway.Api.Services.Interfaces;

public interface IProxyService
{
    Task ForwardAsync(HttpContext context);
}
