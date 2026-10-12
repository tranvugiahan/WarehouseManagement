using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class StockMovement
{
    public long Id { get; set; }

    public int ProductId { get; set; }

    public string Type { get; set; } = null!;

    public string ReferenceType { get; set; } = null!;

    public int ReferenceId { get; set; }

    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
