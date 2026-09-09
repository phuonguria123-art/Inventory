using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Inventory.Dto;

public sealed class InventoryMovementRequestDto
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
