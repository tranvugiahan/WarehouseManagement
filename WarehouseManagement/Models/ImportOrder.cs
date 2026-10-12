using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class ImportOrder
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int SupplierId { get; set; }

    public int CreatedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public DateOnly ImportDate { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ImportOrderDetail> ImportOrderDetails { get; set; } = new List<ImportOrderDetail>();

    public virtual Supplier Supplier { get; set; } = null!;
}
