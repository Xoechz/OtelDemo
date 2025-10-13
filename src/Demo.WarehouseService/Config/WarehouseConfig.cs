using System.Collections.ObjectModel;

namespace Demo.WarehouseService.Config;

public class WarehouseConfig
{
    #region Public Constructors

    public WarehouseConfig(int serviceIndex, int warehouseCount, IDictionary<int, string> redirectionUris)
    {
        ServiceIndex = serviceIndex;
        WarehouseCount = warehouseCount;
        redirectionUris.Remove(serviceIndex);
        RedirectionUrls = new ReadOnlyDictionary<int, string>(redirectionUris);
    }

    #endregion Public Constructors

    #region Public Properties

    public int ServiceIndex { get; }

    public int WarehouseCount { get; }

    public ReadOnlyDictionary<int, string> RedirectionUrls { get; }

    #endregion Public Properties
}