using System.Text.Json;

const int ORDER_SERVICE_COUNT = 2;
const int WAREHOUSE_SERVICE_COUNT = 5;
const int SUPPLIER_SERVICE_COUNT = 2;

var builder = DistributedApplication.CreateBuilder(args);

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
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
        .WithReference(demoDb)
        .WaitFor(demoDb);

    var warehouseService = builder.AddProject<Projects.Demo_WarehouseService>(serviceName)
         .WithIconName("HomeDatabase")
         .WithEnvironment("SERVICE_NAME", serviceName)
         .WithEnvironment("SERVICE_INDEX", serviceIndex.ToString())
         .WithEnvironment("WAREHOUSE_COUNT", WAREHOUSE_SERVICE_COUNT.ToString())
         .WithEnvironment("REDIRECTION_URLS", urls)
         .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
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
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
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
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
        .WithReference(db)
        .WaitFor(db);

    foreach (var warehouseService in warehouseServices)
    {
        supplierService.WaitFor(warehouseService);
    }
}

await builder.Build().RunAsync();