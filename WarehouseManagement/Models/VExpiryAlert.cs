using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class VExpiryAlert
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int ImportOrderId { get; set; }

    public string ImportCode { get; set; } = null!;

    public DateOnly ImportDate { get; set; }

    public int ImportDetailId { get; set; }

    public int ImportedQuantity { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public int? DaysToExpiry { get; set; }

    public int CurrentStock { get; set; }
}
