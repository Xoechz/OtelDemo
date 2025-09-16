using Demo.Data;
using Demo.MigrationService;
using Demo.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var serviceIndex = builder.Configuration["SERVICE_INDEX"]
    ?? throw new InvalidOperationException();

var connectionString = builder.Configuration.GetConnectionString("DB-" + serviceIndex)
?? throw new InvalidOperationException("Connection String DB-" + serviceIndex + " is not configured.");

builder.Services.AddDbContext<DemoContext>(options => options.UseSqlServer(connectionString));

var activitySource = new ActivitySource("placeholder");
builder.Services.AddSingleton(activitySource);

var host = builder.Build();
await host.RunAsync();