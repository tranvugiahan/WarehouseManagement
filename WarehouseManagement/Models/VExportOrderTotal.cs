using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VExportOrderTotal
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Destination { get; set; }

    public DateOnly ExportDate { get; set; }

    public string Status { get; set; } = null!;

    public int? LineCount { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }
}
