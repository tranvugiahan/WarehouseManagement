using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VImportOrderTotal
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public DateOnly ImportDate { get; set; }

    public string Status { get; set; } = null!;

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public int? LineCount { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }
}
