using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = null!;

    public string? Barcode { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public int UnitId { get; set; }

    public decimal CostPrice { get; set; }

    public decimal ExportPrice { get; set; }

    public int MinimumStock { get; set; }

    public bool HasExpiry { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ExportOrderDetail> ExportOrderDetails { get; set; } = new List<ExportOrderDetail>();

    public virtual ICollection<ImportOrderDetail> ImportOrderDetails { get; set; } = new List<ImportOrderDetail>();

    public virtual Inventory? Inventory { get; set; }

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StockTakeDetail> StockTakeDetails { get; set; } = new List<StockTakeDetail>();

    public virtual Unit Unit { get; set; } = null!;
}
