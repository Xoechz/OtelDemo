using System.Diagnostics;
using Demo.Data.Repositories;
using Demo.Models;
using Demo.Models.Extensions;
using Demo.Models.Faker;
using Demo.Dito.Extensions;
using Demo.WarehouseService.Config;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly ActivitySource _activitySource = activitySource;
    private readonly ItemRepository _itemRepository = itemRepository;
    private readonly ILogger<ItemController> _logger = logger;
    private readonly WarehouseConfig _config = config;
    private readonly FailureFaker _failureFaker = failureFaker;
    private readonly Random _rand = new();

    #endregion Private Fields

    #region Public Methods

    [HttpPost("add-stock")]
    public async Task AddStockAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "AddStock");
        var redirectTask = RedirectItemsAsync(redirectedItems, "add-stock");
        var addTask = _itemRepository.AddStockAsync(acceptedItems);
        await Task.WhenAll(redirectTask, addTask);
    }

    [HttpPost("get-items")]
    public async Task<List<Item>> GetItemsAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "GetItems");

        var redirectTask = RedirectItemsAsync(redirectedItems, "get-items");
        var getTask = _itemRepository.GetItemsFromOrderAsync(acceptedItems);
        await Task.WhenAll(redirectTask, getTask);

        var dbItems = getTask.Result;

        if (redirectTask.Result is not null)
        {
            var retrievedItems = await redirectTask.Result.Content.ReadFromJsonAsync<List<Item>>();
            if (retrievedItems is not null)
            {
                dbItems.AddRange(retrievedItems);
            }
        }

        return dbItems;
    }

    #endregion Public Methods

    #region Private Methods

    private (List<Item>, List<Item>) SplitItems(IEnumerable<Item> items, string operation)
    {
        List<Item> acceptedItems = [];
        List<Item> redirectedItems = [];

        foreach (var item in items.Deduplicate())
        {
            var activity = _activitySource.StartEntityActivity(operation, item.ArticleName);

            if (_rand.NextDouble() < 0.5)
            {
                redirectedItems.Add(item);
            }
            else
            {
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

    private async Task<HttpResponseMessage?> RedirectItemsAsync(List<Item> items, string operation)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var redirectIndex = _rand.Next(_config.WarehouseCount);
        redirectIndex = redirectIndex == _config.ServiceIndex ? (redirectIndex + 1) % _config.WarehouseCount : redirectIndex;
        var redirectUrl = _config.RedirectionUrls[redirectIndex];
        _logger.LogWarning("Redirecting {Count} items to WarehouseService-{redirectIndex} for operation {Operation}", items.Count, redirectIndex, operation);

        var response = await _httpClient.PostAsJsonAsync($"{redirectUrl}/item/{operation}", items);
        response.EnsureSuccessStatusCode();
        return response;
    }

    #endregion Private Methods
}