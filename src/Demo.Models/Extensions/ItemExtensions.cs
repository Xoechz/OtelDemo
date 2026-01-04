namespace Demo.Models.Extensions;

public static class ItemExtensions
{
    #region public methods

    public static IEnumerable<Item> Deduplicate(this IEnumerable<Item> orders)
    {
        return orders
            .GroupBy(o => o.ArticleName)
            .Select(g => new Item(g.Key, g.Sum(o => o.Stock)));
    }

    #endregion 
}
