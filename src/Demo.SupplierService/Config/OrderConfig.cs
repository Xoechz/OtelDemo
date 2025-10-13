using System.Collections.ObjectModel;

namespace Demo.SupplierService.Config;

public class SupplierConfig(int serviceIndex, int warehouseCount, IDictionary<int, string> redirectionUrls)
{
    #region Public Properties

    public ReadOnlyDictionary<int, string> RedirectionUrls { get; set; } = new(redirectionUrls);

    public int ServiceIndex { get; set; } = serviceIndex;

    public int WarehouseCount { get; set; } = warehouseCount;

    #endregion Public Properties
}