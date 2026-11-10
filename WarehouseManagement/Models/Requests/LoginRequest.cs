using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

public sealed class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
