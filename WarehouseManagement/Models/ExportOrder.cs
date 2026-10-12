using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class ExportOrder
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Destination { get; set; }

    public DateOnly ExportDate { get; set; }

    public int CreatedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public string? Reason { get; set; }

    public string? RejectionReason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ExportOrderDetail> ExportOrderDetails { get; set; } = new List<ExportOrderDetail>();
}
