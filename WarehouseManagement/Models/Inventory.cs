using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class Inventory
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
