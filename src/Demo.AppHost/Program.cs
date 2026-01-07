using System.Text.Json;

const int ORDER_SERVICE_COUNT = 2;
const int WAREHOUSE_SERVICE_COUNT = 5;
const int SUPPLIER_SERVICE_COUNT = 2;

const string OTEL_COLLECTOR_ENDPOINT = "http://localhost:4317";

var builder = DistributedApplication.CreateBuilder(args);

builder.AddContainer("lgtm", "grafana/otel-lgtm")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithIconName("Eye")
    .WithEndpoint(24317, 4317, "http", "otel-grpc")
    .WithEndpoint(24318, 4318, "http", "otel-http")
    .WithEndpoint(3000, 3000, "http", "grafana-ui");

// There is currently an issue that the collector cannot send telemetry to the apphost, so only grafana is receiving telemetry.
var dito = builder.AddContainer("dito", "xoechz/dito")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithIconName("ShowGrid")
    .WithEndpoint(4317, 4317, "http", "otel-grpc")
    .WithEndpoint(4318, 4318, "http", "otel-http")
    .WithEndpoint(13133, 13133, "http", "health-check")
    .WithEndpoint(55679, 55679, "http", "zpages")
    .WithEndpoint(1777, 1777, "http", "pprof")
    .WithHttpHealthCheck(endpointName: "health-check");

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var warehouseServiceIds = Enumerable.Range(0, WAREHOUSE_SERVICE_COUNT);
var redirectionUrlDict = warehouseServiceIds.ToDictionary(
    i => i,
    i => "https://localhost:" + (5000 + i));
var urls = JsonSerializer.Serialize(redirectionUrlDict);

List<IResourceBuilder<ProjectResource>> warehouseServices = [];
List<IResourceBuilder<SqlServerDatabaseResource>> databases = [];

foreach (var serviceIndex in warehouseServiceIds)
{
    var serviceName = "Warehouse-" + serviceIndex;
    var demoDb = sql.AddDatabase("DB-" + serviceIndex);

    databases.Add(demoDb);

    var migration = builder.AddProject<Projects.Demo_MigrationService>("Migration-" + serviceIndex)
        .WithIconName("Wrench")
        .WithEnvironment("SERVICE_NAME", "Migration-" + serviceIndex)
        .WithEnvironment("SERVICE_INDEX", serviceIndex.ToString())
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_COLLECTOR_ENDPOINT)
        .WithReference(demoDb)
        .WaitFor(demoDb);

    var warehouseService = builder.AddProject<Projects.Demo_WarehouseService>(serviceName)
         .WithIconName("HomeDatabase")
         .WithEnvironment("SERVICE_NAME", serviceName)
         .WithEnvironment("SERVICE_INDEX", serviceIndex.ToString())
         .WithEnvironment("WAREHOUSE_COUNT", WAREHOUSE_SERVICE_COUNT.ToString())
         .WithEnvironment("REDIRECTION_URLS", urls)
         .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_COLLECTOR_ENDPOINT)
         .WithReference(demoDb)
         .WaitForCompletion(migration);

    warehouseServices.Add(warehouseService);
}

for (var i = 0; i < ORDER_SERVICE_COUNT; i++)
{
    var db = databases.ElementAtOrDefault(i) ?? sql.AddDatabase("DB-" + i);
    databases.Add(db);

    var orderService = builder.AddProject<Projects.Demo_OrderService>("OrderService-" + i)
        .WithIconName("Cart")
        .WithEnvironment("SERVICE_NAME", "OrderService-" + i)
        .WithEnvironment("SERVICE_INDEX", i.ToString())
        .WithEnvironment("WAREHOUSE_COUNT", WAREHOUSE_SERVICE_COUNT.ToString())
        .WithEnvironment("REDIRECTION_URLS", urls)
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_COLLECTOR_ENDPOINT)
        .WithReference(db)
        .WaitFor(db);

    foreach (var warehouseService in warehouseServices)
    {
        orderService.WaitFor(warehouseService);
    }
}

for (var i = 0; i < SUPPLIER_SERVICE_COUNT; i++)
{
    var db = databases.ElementAtOrDefault(i) ?? sql.AddDatabase("DB-" + i);
    databases.Add(db);

    var supplierService = builder.AddProject<Projects.Demo_SupplierService>("SupplierService-" + i)
        .WithIconName("Settings")
        .WithEnvironment("SERVICE_NAME", "SupplierService-" + i)
        .WithEnvironment("SERVICE_INDEX", i.ToString())
        .WithEnvironment("WAREHOUSE_COUNT", WAREHOUSE_SERVICE_COUNT.ToString())
        .WithEnvironment("REDIRECTION_URLS", urls)
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_COLLECTOR_ENDPOINT)
        .WithReference(db)
        .WaitFor(db);

    foreach (var warehouseService in warehouseServices)
    {
        supplierService.WaitFor(warehouseService);
    }
}

await builder.Build().RunAsync();
