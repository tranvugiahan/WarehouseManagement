using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<ExportOrder> ExportOrderApprovedByNavigations { get; set; } = new List<ExportOrder>();

    public virtual ICollection<ExportOrder> ExportOrderCreatedByNavigations { get; set; } = new List<ExportOrder>();

    public virtual ICollection<ImportOrder> ImportOrderApprovedByNavigations { get; set; } = new List<ImportOrder>();

    public virtual ICollection<ImportOrder> ImportOrderCreatedByNavigations { get; set; } = new List<ImportOrder>();

    public virtual ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StockTake> StockTakeApprovedByNavigations { get; set; } = new List<StockTake>();

    public virtual ICollection<StockTake> StockTakeCreatedByNavigations { get; set; } = new List<StockTake>();
}
