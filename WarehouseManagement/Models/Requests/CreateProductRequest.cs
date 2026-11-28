using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.Requests;

public class CreateProductRequest : IValidatableObject
{
    [Required, StringLength(50), RegularExpression(@".*\S.*", ErrorMessage = "SKU không được để trống.")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Barcode { get; set; }

    [Required, StringLength(200), RegularExpression(@".*\S.*", ErrorMessage = "Tên sản phẩm không được để trống.")]
    public string Name { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int? UnitId { get; set; }

    [Required, Range(typeof(decimal), "0", "9999999999999.99")]
    public decimal? CostPrice { get; set; }

    [Required, Range(typeof(decimal), "0", "9999999999999.99")]
    public decimal? ExportPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int? MinimumStock { get; set; }

    [Required]
    public bool? HasExpiry { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CostPrice is { } costPrice && decimal.Round(costPrice, 2) != costPrice)
            yield return new ValidationResult("Giá nhập chỉ được có tối đa 2 chữ số thập phân.", [nameof(CostPrice)]);

        if (ExportPrice is { } exportPrice && decimal.Round(exportPrice, 2) != exportPrice)
            yield return new ValidationResult("Giá xuất chỉ được có tối đa 2 chữ số thập phân.", [nameof(ExportPrice)]);
    }
}

public sealed class UpdateProductRequest : CreateProductRequest;

public sealed record ProductResponse(int Id, string Sku, string? Barcode, string Name,
    int CategoryId, int UnitId, decimal CostPrice, decimal ExportPrice,
    int MinimumStock, bool HasExpiry, string Status, DateTime CreatedAt);
