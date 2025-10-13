using Demo.Models.Faker;
using Demo.OrderService;
using Demo.OrderService.Config;
using Demo.ServiceDefaults;
using Hangfire;
using Hangfire.Common;
using Hangfire.SqlServer;
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

builder.Services.AddSingleton(new OrderConfig(serviceIndex, warehouseCount, urlsDict));

// Enable HTTP/3 (alongside HTTP/1.1 & HTTP/2) on the chosen port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5100 + serviceIndex, lo =>
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

builder.Services.AddSingleton(new ItemFaker(serviceIndex + 100));

builder.Services.AddHangfireServer()
    .AddTransient<OrderWorker>()
    .AddHangfire(opt => opt.UseSqlServerStorage(connectionString,
    new SqlServerStorageOptions
    {
        SchemaName = "HangfireOrder",
        QueuePollInterval = TimeSpan.FromSeconds(15),
    }));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapHangfireDashboard();

var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
jobManager.RemoveIfExists("OrderService");
jobManager.AddOrUpdate("OrderService", new Job(typeof(OrderWorker).GetMethod(nameof(OrderWorker.DoWork)), typeof(OrderWorker)), $"*/{serviceIndex + 1} * * * *");
jobManager.Trigger("OrderService");

await app.RunAsync();