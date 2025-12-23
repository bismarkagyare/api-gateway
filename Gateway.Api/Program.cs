using Gateway.Api.Services.Implementations;
using Gateway.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register MVC controllers so attribute-routed controllers are discovered
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IProxyService, ProxyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map attribute-routed controllers (e.g. HealthController)
app.MapControllers();

app.Run();
