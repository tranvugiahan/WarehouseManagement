using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VDamageExpiredReport
{
    public int ExportOrderId { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public DateOnly ExportDate { get; set; }

    public string? Reason { get; set; }

    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal? Amount { get; set; }
}
