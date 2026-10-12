using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

// Only fields accepted from the client; navigation properties are excluded.
public class CreateCategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

}

public class UpdateCategoryRequest : CreateCategoryRequest
{
    [Required, RegularExpression("^(ACTIVE|INACTIVE)$")]
    public string Status { get; set; } = string.Empty;
}