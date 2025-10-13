using Demo.Dito.Extensions;
using Demo.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Demo.Data.Repositories;

public class ItemRepository(DemoContext demoContext,
                            ActivitySource activitySource)
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly DemoContext _context = demoContext;

    #endregion Private Fields

    #region Public Methods

    public async Task<bool> AddStockAsync(IEnumerable<Order> items)
    {
        var existingItems = await _context.Items
            .Where(dbItem => items.Select(i => i.ArticleName).Contains(dbItem.ArticleName))
            .ToListAsync();

        foreach (var item in items)
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
        return true;
    }

    public async Task<List<Order>> GetItemsFromOrderAsync(IEnumerable<Order> items)
    {
        List<Order> result = [];
        var dbItems = await _context.Items
             .Where(dbItem => items.Select(i => i.ArticleName).Contains(dbItem.ArticleName))
             .ToListAsync();

        foreach (var item in items)
        {
            using var activity = _activitySource.StartEntityActivity("GetItemsFromOrderAsync", item.ArticleName);
            activity?.SetTag("item.requested", item.Stock);

            var dbItem = dbItems.FirstOrDefault(di => di.ArticleName == item.ArticleName);
            if (dbItem is not null)
            {
                activity?.SetTag("item.available", dbItem.Stock);

                if (dbItem.Stock >= item.Stock)
                {
                    result.Add(new Order(item.ArticleName, item.Stock));
                    dbItem.Stock -= item.Stock;
                    activity?.SetStatus(ActivityStatusCode.Ok, "Item is in stock");
                }
                else
                {
                    result.Add(new Order(item.ArticleName, dbItem.Stock));
                    dbItem.Stock = 0;
                    activity?.SetStatus(ActivityStatusCode.Error, "Not enough stock available");
                }
            }
            else
            {
                activity?.SetTag("item.available", 0);
                activity?.SetStatus(ActivityStatusCode.Error, "Item not found in inventory");
            }
        }

        await _context.SaveChangesAsync();

        return result;
    }

    #endregion Public Methods
}