using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VLowStockAlert
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string? Barcode { get; set; }

    public string ProductName { get; set; } = null!;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string UnitName { get; set; } = null!;

    public string ProductStatus { get; set; } = null!;

    public int Quantity { get; set; }

    public int MinimumStock { get; set; }

    public bool? IsLowStock { get; set; }

    public decimal CostPrice { get; set; }

    public decimal? StockValue { get; set; }

    public DateTime UpdatedAt { get; set; }
}
