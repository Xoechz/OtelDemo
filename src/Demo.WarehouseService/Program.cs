using Demo.Data;
using Demo.Data.Repositories;
using Demo.Models.Faker;
using Demo.ServiceDefaults;
using Demo.WarehouseService.Config;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var serviceIndex = builder.Configuration.GetValue<int?>("SERVICE_INDEX")
    ?? throw new InvalidOperationException("SERVICE_INDEX is not configured correctly.");

var connectionString = builder.Configuration.GetConnectionString("DB-" + serviceIndex)
    ?? throw new InvalidOperationException("Connection String DB-" + serviceIndex + " is not configured.");

var urls = builder.Configuration["REDIRECTION_URLS"]
    ?? throw new InvalidOperationException("REDIRECTION_URLS is not configured.");

var urlsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, string>>(urls)
    ?? throw new InvalidOperationException("REDIRECTION_URLS is not configured correctly.");

var warehouseCount = builder.Configuration.GetValue<int?>("WAREHOUSE_COUNT")
    ?? throw new InvalidOperationException("WAREHOUSE_COUNT is not configured correctly.");

builder.Services.AddSingleton(new WarehouseConfig(serviceIndex, warehouseCount, urlsDict));

builder.Services.AddDbContext<DemoContext>(options => options.UseSqlServer(connectionString));

// Enable HTTP/3 (alongside HTTP/1.1 & HTTP/2) on the chosen port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000 + serviceIndex, lo =>
    {
        lo.UseHttps();
        lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

builder.Services.AddHttpClient("client", client =>
{
    client.DefaultRequestVersion = HttpVersion.Version30;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    AutomaticDecompression = DecompressionMethods.All,
});

builder.Services.AddSingleton(new FailureFaker(serviceIndex));

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ItemRepository>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();