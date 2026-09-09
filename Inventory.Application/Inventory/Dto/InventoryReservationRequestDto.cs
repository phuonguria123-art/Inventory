using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Inventory.Dto;

public sealed class InventoryReservationRequestDto
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
    public bool IsCancel { get; set; }
}
