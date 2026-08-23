using Gateway.Api.Configuration;
using Gateway.Api.Middleware;
using Gateway.Api.Services.Implementations;
using Gateway.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register MVC controllers so attribute-routed controllers are discovered
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<DownstreamHealthCheck>("downstream");

builder.Services.AddSingleton<IProxyService, ProxyService>();
builder.Services.AddSingleton<IRateLimitService, RedisRateLimitService>();
// ApiKeyService is stateless (wraps the singleton Redis multiplexer) and is
// constructor-injected into middleware, which is resolved from the root
// provider — so it must be a singleton, not scoped.
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();

// Register Redis connection. AbortOnConnectFail=false lets the gateway start and
// retry in the background even if Redis is temporarily unreachable; health checks
// report the degraded state instead of the process crashing on startup.
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

    var redisConfig = ConfigurationOptions.Parse(options.ConnectionString);
    redisConfig.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(redisConfig);
});

builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

builder.Services.Configure<DownstreamOptions>(builder.Configuration.GetSection("Downstream"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseHttpsRedirection();

// Map attribute-routed controllers (e.g. ProxyController)
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
