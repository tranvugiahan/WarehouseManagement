using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class ExportOrderDetail
{
    public int Id { get; set; }

    public int ExportOrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual ExportOrder ExportOrder { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
