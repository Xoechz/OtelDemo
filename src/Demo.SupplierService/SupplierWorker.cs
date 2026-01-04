using System.Diagnostics;
using Demo.Models.Faker;
using Demo.SupplierService.Config;

namespace Demo.SupplierService;

public class SupplierWorker(ActivitySource activitySource,
                         SupplierConfig config,
                         ItemFaker itemFaker,
                         IHttpClientFactory httpClientFactory)
{
    #region public methods

    public async Task DoWork(CancellationToken cancellationToken)
    {
        var randomIndex = _rand.Next(0, _config.WarehouseCount);

        using var activity = _activitySource.StartActivity("SupplierService.DoWork");

        var targetUrl = _config.RedirectionUrls[randomIndex]
            ?? throw new InvalidOperationException("No target URL provided");

        var requestedItems = _jobFaker.Generate(_rand.Next(10, 20));
        activity?.SetTag("item.supplied.distinct", requestedItems.Count);
        activity?.SetTag("item.supplied.total", requestedItems.Sum(i => i.Stock));

        var response = await _httpClient.PostAsJsonAsync($"{targetUrl}/item/add-stock", requestedItems, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion 

    #region private fields
    private readonly ActivitySource _activitySource = activitySource;
    private readonly SupplierConfig _config = config;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly ItemFaker _jobFaker = itemFaker;
    private readonly Random _rand = new();
    #endregion 
}
