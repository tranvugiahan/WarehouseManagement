namespace WarehouseManagement.Models;

public sealed class RefreshSession
{
    public string Id { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
