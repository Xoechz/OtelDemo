using System.Diagnostics;
using Demo.Data.Repositories;
using Demo.Dito.Extensions;
using Demo.Models;
using Demo.Models.Extensions;
using Demo.Models.Faker;
using Demo.OpenTelemetry;
using Demo.WarehouseService.Config;
using Microsoft.AspNetCore.Mvc;

namespace Demo.WarehouseService.Controller;

[ApiController]
[Route("[controller]")]
public class ItemController(ItemRepository itemRepository,
                            WarehouseConfig config,
                            ILogger<ItemController> logger,
                            ActivitySource activitySource,
                            IHttpClientFactory httpClientFactory,
                            FailureFaker failureFaker,
                            MetricInstruments instruments)
    : ControllerBase
{
    #region public methods

    [HttpPost("add-stock")]
    public async Task AddStockAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "Checking item for adding");
        var addTask = _itemRepository.AddStockAsync(acceptedItems);
        var redirectTasks = RedirectItems(redirectedItems, "add-stock");
        await Task.WhenAll(Task.WhenAll(redirectTasks), addTask);
    }

    [HttpPost("get-items")]
    public async Task<List<Item>> GetItemsAsync([FromBody] IEnumerable<Item> items)
    {
        var (acceptedItems, redirectedItems) = SplitItems(items, "Checking item for order");
        var getTask = _itemRepository.GetItemsForOrderAsync(acceptedItems);
        var redirectTasks = RedirectItems(redirectedItems, "get-items");
        await Task.WhenAll(Task.WhenAll(redirectTasks), getTask);

        var dbItems = getTask.Result;

        using (var activity = _activitySource.StartActivity("Merge order results"))
        {
            foreach (var response in redirectTasks)
            {
                var retrievedItems = await response.Result.Content.ReadFromJsonAsync<List<Item>>();
                if (retrievedItems is not null)
                {
                    dbItems.AddRange(retrievedItems);
                }
            }
        }

        return dbItems;
    }

    #endregion 

    #region private fields
    private readonly ActivitySource _activitySource = activitySource;
    private readonly WarehouseConfig _config = config;
    private readonly FailureFaker _failureFaker = failureFaker;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("client");
    private readonly MetricInstruments _instruments = instruments;
    private readonly ItemRepository _itemRepository = itemRepository;
    private static readonly Dictionary<string, (ActivityTraceId traceId, ActivitySpanId spanId)> _lastOperationPerItem = [];
    private readonly ILogger<ItemController> _logger = logger;
    private readonly Random _rand = new();
    #endregion 

    #region private methods

    private int GetRedirectionIndex()
    {
        var redirectIndex = _rand.Next(_config.WarehouseCount);
        return redirectIndex == _config.ServiceIndex ? (redirectIndex + 1) % _config.WarehouseCount : redirectIndex;
    }

    private List<Task<HttpResponseMessage>> RedirectItems(List<Item> items, string operation)
    {
        List<Task<HttpResponseMessage>> responses = [];

        var redirections = items
            .GroupBy(item => GetRedirectionIndex())
            .Where(g => g.Any())
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (redirectIndex, redirectedItems) in redirections)
        {
            var redirectUrl = _config.RedirectionUrls[redirectIndex];
            _logger.LogWarning("Redirecting {Count} items to WarehouseService-{RedirectIndex} for operation {Operation}", redirectedItems.Count, redirectIndex, operation);

            responses.Add(_httpClient.PostAsJsonAsync($"{redirectUrl}/item/{operation}", redirectedItems)
                .ContinueWith(t => t.Result.EnsureSuccessStatusCode()));
        }

        return responses;
    }

    private (List<Item>, List<Item>) SplitItems(IEnumerable<Item> items, string operation)
    {
        List<Item> acceptedItems = [];
        List<Item> redirectedItems = [];

        _instruments.ItemsProcessedHistogram.Record(items.Count(), [new("operation", operation)]);

        foreach (var item in items.Deduplicate())
        {
            if (_rand.NextDouble() < 0.5)
            {
                _instruments.ItemsProcessedCounter.Add(1, new("operation", operation), new("outcome", "redirected"));
                redirectedItems.Add(item);
            }
            else
            {
                using var activity = _activitySource.StartEntityActivity(operation, item.ArticleName);
                activity?.SetTag("item.amount", item.Stock);

                var failure = _failureFaker.Generate();
                if (failure is not null)
                {
                    _instruments.ItemsProcessedCounter.Add(1, new("operation", operation), new("outcome", "failure"));
                    _logger.LogWarning("Failed operation {Operation} on item {Item} to stock: {Failure}", operation, item.ArticleName, failure);
                    activity?.SetStatus(ActivityStatusCode.Error, failure);
                    activity?.AddEvent(new ActivityEvent("Operation failed", tags: new ActivityTagsCollection { { "failure.reason", failure } }));
                }
                else
                {
                    _instruments.ItemsProcessedCounter.Add(1, new("operation", operation), new("outcome", "accepted"));
                    acceptedItems.Add(item);
                    activity?.SetStatus(ActivityStatusCode.Ok, "Item accepted");
                }

                if (activity is not null)
                {
                    if (_lastOperationPerItem.TryGetValue(item.ArticleName, out var lastOpIds))
                    {
                        var link = new ActivityLink(new ActivityContext(lastOpIds.traceId, lastOpIds.spanId, ActivityTraceFlags.None));
                        activity.AddLink(link);
                    }
                    _lastOperationPerItem[item.ArticleName] = (activity.TraceId, activity.SpanId);
                }
            }
        }

        return (acceptedItems, redirectedItems);
    }

    #endregion 
}
