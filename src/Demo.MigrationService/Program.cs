using Demo.Data;
using Demo.MigrationService;
using Demo.Models.Faker;
using Demo.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var serviceIndex = builder.Configuration["SERVICE_INDEX"]
    ?? throw new InvalidOperationException();

if (!int.TryParse(serviceIndex, out var serviceIndexValue))
{
    throw new InvalidOperationException("SERVICE_INDEX is not an integer.");
}

var connectionString = builder.Configuration.GetConnectionString("DB-" + serviceIndex)
?? throw new InvalidOperationException("Connection String DB-" + serviceIndex + " is not configured.");

builder.Services.AddDbContext<DemoContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton(new ItemFaker(serviceIndexValue));

var host = builder.Build();
await host.RunAsync();
