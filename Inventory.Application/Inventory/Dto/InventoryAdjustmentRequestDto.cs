using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Inventory.Dto;

public sealed class InventoryAdjustmentRequestDto
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }

    [Range(0, int.MaxValue)]
    public int ActualQuantity { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public required string Notes { get; set; }
}
