using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VExportByDestination
{
    public string? Destination { get; set; }

    public string? ExportMonth { get; set; }

    public DateOnly ExportDate { get; set; }

    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int? TotalQuantity { get; set; }

    public decimal? TotalAmount { get; set; }
}
