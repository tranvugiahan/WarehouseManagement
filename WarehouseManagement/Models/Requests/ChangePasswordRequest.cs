using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
