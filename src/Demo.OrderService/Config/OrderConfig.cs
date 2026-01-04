using System.Collections.ObjectModel;

namespace Demo.OrderService.Config;

public class OrderConfig(int serviceIndex, int warehouseCount, IDictionary<int, string> redirectionUrls)
{
    #region public properties
    public ReadOnlyDictionary<int, string> RedirectionUrls { get; set; } = new(redirectionUrls);
    public int ServiceIndex { get; set; } = serviceIndex;
    public int WarehouseCount { get; set; } = warehouseCount;
    #endregion 
}
