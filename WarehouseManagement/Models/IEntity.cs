namespace WarehouseManagement.Models;

public interface IEntity
{
    int Id { get; set; }
    string Status { get; set; }
}