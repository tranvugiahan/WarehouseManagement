using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string Module { get; set; } = null!;

    public long? ReferenceId { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
