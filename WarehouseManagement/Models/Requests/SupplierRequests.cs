using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

// Only fields accepted from the client; navigation properties are excluded.
public class CreateSupplierRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [StringLength(20)]
    public string? Phone { get; set; }
    [StringLength(150)]
    public string? Email { get; set; }
    [StringLength(255)]
    public string? Address { get; set; }
}

public class UpdateSupplierRequest : CreateSupplierRequest
{
    [Required, RegularExpression("^(ACTIVE|INACTIVE)$")]
    public string Status { get; set; } = string.Empty;
}