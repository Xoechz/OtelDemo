using Demo.Dito.Extensions;
using Demo.Models;
using Demo.Models.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Demo.Data.Repositories;

public class ItemRepository(DemoContext demoContext,
                            ActivitySource activitySource,
                            ILogger<ItemRepository> logger)
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly DemoContext _context = demoContext;
    private readonly ILogger<ItemRepository> _logger = logger;

    #endregion Private Fields

    #region Public Methods

    public async Task AddStockAsync(List<Models.Item> items)
    {
        using var activity = _activitySource.StartActivity("Add stock");
        if (items.Count == 0)
        {
            return;
        }

        var existingItems = await _context.Items
            .Where(dbItem => items.Select(i => i.ArticleName).Contains(dbItem.ArticleName))
            .ToListAsync();

        foreach (var item in items.Deduplicate())
        {
            var dbItem = existingItems.FirstOrDefault(ei => ei.ArticleName == item.ArticleName);
            if (dbItem is not null)
            {
                dbItem.Stock += item.Stock;
            }
            else
            {
                await _context.Items.AddAsync(new Entities.Item
                {
                    ArticleName = item.ArticleName,
                    Stock = item.Stock
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Models.Item>> GetItemsForOrderAsync(IEnumerable<Models.Item> items)
    {
        using var activity = _activitySource.StartActivity("Get items for order");
        List<Item> result = [];
        var dbItems = await _context.Items
             .Where(dbItem => items.Select(i => i.ArticleName).Contains(dbItem.ArticleName))
             .ToListAsync();

        foreach (var item in items.Deduplicate())
        {
            using var entityActivity = _activitySource.StartEntityActivity("Get item for order", item.ArticleName);
            entityActivity?.SetTag("item.requested", item.Stock);

            var dbItem = dbItems.FirstOrDefault(di => di.ArticleName == item.ArticleName);
            if (dbItem is not null)
            {
                entityActivity?.SetTag("item.available", dbItem.Stock);

                if (dbItem.Stock >= item.Stock)
                {
                    result.Add(new Item(item.ArticleName, item.Stock));
                    dbItem.Stock -= item.Stock;
                    _logger.LogInformation("Item {ItemName} is in stock", item.ArticleName);
                    entityActivity?.SetStatus(ActivityStatusCode.Ok, "Item is in stock");
                }
                else
                {
                    result.Add(new Item(item.ArticleName, dbItem.Stock));
                    dbItem.Stock = 0;
                    _logger.LogWarning("Not enough stock available for item {ItemName}", item.ArticleName);
                    entityActivity?.SetStatus(ActivityStatusCode.Error, "Not enough stock available");
                }
            }
            else
            {
                entityActivity?.SetTag("item.available", 0);
                _logger.LogError("Item {ItemName} not found in inventory", item.ArticleName);
                entityActivity?.SetStatus(ActivityStatusCode.Error, "Item not found in inventory");
            }
        }

        await _context.SaveChangesAsync();

        return result;
    }

    #endregion Public Methods
}