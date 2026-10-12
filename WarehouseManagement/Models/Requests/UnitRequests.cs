using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

// Only fields accepted from the client; navigation properties are excluded.
public class CreateUnitRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

}

public class UpdateUnitRequest : CreateUnitRequest
{
    [Required, RegularExpression("^(ACTIVE|INACTIVE)$")]
    public string Status { get; set; } = string.Empty;
}