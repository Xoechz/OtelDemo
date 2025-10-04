using Demo.Data;
using Demo.Data.Models;
using Demo.Data.Repositories;
using Demo.JobService.Config;
using Demo.JobService.Jobs;
using Demo.ServiceDefaults;
using Hangfire;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var serviceIndex = builder.Configuration["SERVICE_INDEX"]
    ?? throw new InvalidOperationException();

var connectionString = builder.Configuration.GetConnectionString("DB-" + serviceIndex)
    ?? throw new InvalidOperationException("Connection String DB-" + serviceIndex + " is not configured.");

builder.Services.AddDbContext<DemoContext>(options => options.UseSqlServer(connectionString));

if (!int.TryParse(serviceIndex, out var serviceIndexValue))
{
    throw new InvalidOperationException("SERVICE_INDEX is not a valid integer.");
}

// Enable HTTP/3 (alongside HTTP/1.1 & HTTP/2) on the chosen port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5280 + serviceIndexValue, lo =>
    {
        lo.UseHttps();
        lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

builder.Services.AddHttpClient("jobs", client =>
{
    client.DefaultRequestVersion = HttpVersion.Version30;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    AutomaticDecompression = DecompressionMethods.All,
});

builder.Services.AddHangfireServer()
    .AddSingleton<RecurringJobScheduler>()
    .AddTransient<JobWorker>()
    .AddHangfire(opt => opt.UseSqlServerStorage(connectionString));

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<UserRepository>();

var urls = builder.Configuration["TARGET_URLS"]
    ?? throw new InvalidOperationException("TARGET_URLS is not configured.");

builder.Services.AddSingleton(new JobConfig
{
    TargetUrls = urls.Split(",").Select(u => u.Trim()),
    CronExpression = builder.Configuration["CRON_EXPRESSION"],
    ErrorChances = builder.Configuration.GetValue<IDictionary<ErrorType, decimal>>("ERROR_CHANCES")
        ?? new Dictionary<ErrorType, decimal>
    {
        { ErrorType.None, 0.99m },
        { ErrorType.Validation, 0.09m },
        { ErrorType.Critical, 0.01m },
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHangfireDashboard();

var scheduler = app.Services.GetRequiredService<RecurringJobScheduler>();
scheduler.ScheduleRecurringJobs();

await app.RunAsync();