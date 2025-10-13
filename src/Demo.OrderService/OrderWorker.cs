using Demo.Dito.Extensions;
using Demo.OrderService.Config;
using Demo.Models.Faker;
using System.Diagnostics;
using Demo.Data.Entities;

namespace Demo.OrderService;

public class OrderWorker(ActivitySource activitySource,
                         OrderConfig config,
                         ItemFaker itemFaker,
                         IHttpClientFactory httpClientFactory)
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly OrderConfig _config = config;
    private readonly ItemFaker _jobFaker = itemFaker;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly Random _rand = new();

    #endregion Private Fields

    #region Public Methods

    public async Task DoWork(CancellationToken cancellationToken)
    {
        var randomIndex = _rand.Next(0, _config.WarehouseCount);

        using var activity = _activitySource.StartJobActivity("OrderService.DoWork", $"Ordering Items",
            source: $"WarehouseService-{randomIndex}",
            destination: $"OrderService-{_config.ServiceIndex}",
            entityType: "Item");

        var targetUrl = _config.RedirectionUrls[randomIndex]
            ?? throw new InvalidOperationException("No target URL provided");

        var requestedItems = _jobFaker.Generate(_rand.Next(10, 20));
        activity?.SetTag("item.requested.distinct", requestedItems.Count);
        activity?.SetTag("item.requested.total", requestedItems.Sum(i => i.Stock));

        var response = await _httpClient.PostAsJsonAsync($"{targetUrl}/item/get-items", requestedItems, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<Item>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("No items returned from warehouse");

        activity?.SetTag("item.retrieved.distinct", items.Count);
        activity?.SetTag("item.retrieved.total", items.Sum(i => i.Stock));
    }

    #endregion Public Methods
}