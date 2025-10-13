using Demo.Dito.Extensions;
using Demo.SupplierService.Config;
using Demo.Models.Faker;
using System.Diagnostics;
using Demo.Data.Entities;

namespace Demo.SupplierService;

public class SupplierWorker(ActivitySource activitySource,
                         SupplierConfig config,
                         ItemFaker itemFaker,
                         IHttpClientFactory httpClientFactory)
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly SupplierConfig _config = config;
    private readonly ItemFaker _jobFaker = itemFaker;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly Random _rand = new();

    #endregion Private Fields

    #region Public Methods

    public async Task DoWork(CancellationToken cancellationToken)
    {
        var randomIndex = _rand.Next(0, _config.WarehouseCount);

        using var activity = _activitySource.StartJobActivity("SupplierService.DoWork", $"Supplying Items",
            source: $"WarehouseService-{randomIndex}",
            destination: $"SupplierService-{_config.ServiceIndex}",
            entityType: "Item");

        var targetUrl = _config.RedirectionUrls[randomIndex]
            ?? throw new InvalidOperationException("No target URL provided");

        var requestedItems = _jobFaker.Generate(_rand.Next(10, 20));
        activity?.SetTag("item.supplied.distinct", requestedItems.Count);
        activity?.SetTag("item.supplied.total", requestedItems.Sum(i => i.Stock));

        var response = await _httpClient.PostAsJsonAsync($"{targetUrl}/item/add-stock", requestedItems, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion Public Methods
}