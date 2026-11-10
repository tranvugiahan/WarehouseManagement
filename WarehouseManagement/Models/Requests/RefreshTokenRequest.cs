using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
