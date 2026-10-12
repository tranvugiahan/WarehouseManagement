using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VStockMovementHistory
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string ReferenceType { get; set; } = null!;

    public int ReferenceId { get; set; }

    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public int CreatedBy { get; set; }

    public string CreatedByName { get; set; } = null!;
}
