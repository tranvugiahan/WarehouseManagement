using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class StockTakeDetail
{
    public int StockTakeId { get; set; }

    public int ProductId { get; set; }

    public int SystemQuantity { get; set; }

    public int? ActualQuantity { get; set; }

    public int? Difference { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual StockTake StockTake { get; set; } = null!;
}
