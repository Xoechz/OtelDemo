using System.Collections.ObjectModel;

namespace Demo.SupplierService.Config;

public class SupplierConfig(int serviceIndex, int warehouseCount, IDictionary<int, string> redirectionUrls)
{
    #region Public Properties

    public int ServiceIndex { get; set; } = serviceIndex;

    public int WarehouseCount { get; set; } = warehouseCount;

    public ReadOnlyDictionary<int, string> RedirectionUrls { get; set; } = new(redirectionUrls);

    #endregion Public Properties
}