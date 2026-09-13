using Inventory.Application.Inventory.Dto.Adjust;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Inventory.Dto;

public sealed class AdjustInventoryRequestDto
{
    public Guid WarehouseId { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [Required]
    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public List<AdjustProductDto> Products { get; set; } = [];
}
