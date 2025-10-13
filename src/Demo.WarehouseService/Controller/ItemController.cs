using Demo.Data.Repositories;
using Demo.Dito.Extensions;
using Demo.Models;
using Demo.Models.Extensions;
using Demo.Models.Faker;
using Demo.WarehouseService.Config;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Demo.WarehouseService.Controller;

[ApiController]
[Route("[controller]")]
public class ItemController(ItemRepository itemRepository,
                            WarehouseConfig config,
                            ILogger<ItemController> logger,
                            ActivitySource activitySource,
                            IHttpClientFactory httpClientFactory,
                            FailureFaker failureFaker)
    : ControllerBase
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly WarehouseConfig _config = config;
    private readonly FailureFaker _failureFaker = failureFaker;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly ItemRepository _itemRepository = itemRepository;
    private readonly ILogger<ItemController> _logger = logger;
    private readonly Random _rand = new();

    #endregion Private Fields

    #region Public Methods

    [HttpPost("add-stock")]
    public async Task AddStockAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "Checking item for adding");
        var addTask = _itemRepository.AddStockAsync(acceptedItems);
        var redirectTask = RedirectItemsAsync(redirectedItems, "add-stock");
        await Task.WhenAll(redirectTask, addTask);
    }

    [HttpPost("get-items")]
    public async Task<List<Item>> GetItemsAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "Checking item for order");
        var getTask = _itemRepository.GetItemsForOrderAsync(acceptedItems);
        var redirectTask = RedirectItemsAsync(redirectedItems, "get-items");
        await Task.WhenAll(redirectTask, getTask);

        var dbItems = getTask.Result;

        using (var activity = _activitySource.StartActivity("Merge order results"))
        {
            if (redirectTask.Result is not null)
            {
                var retrievedItems = await redirectTask.Result.Content.ReadFromJsonAsync<List<Item>>();
                if (retrievedItems is not null)
                {
                    dbItems.AddRange(retrievedItems);
                }
            }
        }

        return dbItems;
    }

    #endregion Public Methods

    #region Private Methods

    private async Task<HttpResponseMessage?> RedirectItemsAsync(List<Item> items, string operation)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var redirectIndex = _rand.Next(_config.WarehouseCount);
        redirectIndex = redirectIndex == _config.ServiceIndex ? (redirectIndex + 1) % _config.WarehouseCount : redirectIndex;
        var redirectUrl = _config.RedirectionUrls[redirectIndex];
        _logger.LogWarning("Redirecting {Count} items to WarehouseService-{RedirectIndex} for operation {Operation}", items.Count, redirectIndex, operation);

        var response = await _httpClient.PostAsJsonAsync($"{redirectUrl}/item/{operation}", items);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private (List<Item>, List<Item>) SplitItems(IEnumerable<Item> items, string operation)
    {
        List<Item> acceptedItems = [];
        List<Item> redirectedItems = [];

        foreach (var item in items.Deduplicate())
        {
            if (_rand.NextDouble() < 0.5)
            {
                redirectedItems.Add(item);
            }
            else
            {
                using var activity = _activitySource.StartEntityActivity(operation, item.ArticleName);

                var failure = _failureFaker.Generate();
                if (failure is not null)
                {
                    _logger.LogWarning("Failed operation {Operation} on item {Item} to stock: {Failure}", operation, item.ArticleName, failure);
                    activity?.SetStatus(ActivityStatusCode.Error, failure);
                }
                else
                {
                    acceptedItems.Add(item);
                    activity?.SetStatus(ActivityStatusCode.Ok, "Item accepted");
                }
            }
        }

        return (acceptedItems, redirectedItems);
    }

    #endregion Private Methods
}