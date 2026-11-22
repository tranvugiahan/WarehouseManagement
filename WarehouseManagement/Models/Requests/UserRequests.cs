using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

public sealed class CreateUserRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50), RegularExpression(@"^[^@\s]+$", ErrorMessage = "Username không được chứa khoảng trắng hoặc ký tự @.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(ADMIN|MANAGER|EMPLOYEE)$", ErrorMessage = "Role phải là ADMIN, MANAGER hoặc EMPLOYEE.")]
    public string Role { get; set; } = string.Empty;
}

public sealed record CreatedUserResponse(int Id, string FullName, string Username, string Email,
    string Role, string Status);

public sealed class UpdateUserRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50), RegularExpression(@"^[^@\s]+$", ErrorMessage = "Username không được chứa khoảng trắng hoặc ký tự @.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression("^(ADMIN|MANAGER|EMPLOYEE)$", ErrorMessage = "Role phải là ADMIN, MANAGER hoặc EMPLOYEE.")]
    public string Role { get; set; } = string.Empty;
}
