using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models;

public partial class StatusHistory
{
    public long Id { get; set; }

    public string DocumentType { get; set; } = null!;

    public int DocumentId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Reason { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;
}
