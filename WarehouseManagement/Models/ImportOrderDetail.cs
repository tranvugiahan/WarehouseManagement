using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class ImportOrderDetail
{
    public int Id { get; set; }

    public int ImportOrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public virtual ImportOrder ImportOrder { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
