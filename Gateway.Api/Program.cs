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

builder.Services.AddSingleton<IProxyService, ProxyService>();
builder.Services.AddSingleton<IRateLimitService, RedisRateLimitService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(options.ConnectionString);
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

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseHttpsRedirection();

// Map attribute-routed controllers (e.g. HealthController)
app.MapControllers();

app.Run();
