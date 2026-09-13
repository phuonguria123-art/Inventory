using Inventory.Application.Inventory.Dto.Reservation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Application.Inventory.Dto;

public sealed class ReserveInventoryRequestDto
{
    public Guid WarehouseId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
    [JsonPropertyName("isCancel")]
    public bool IsCancellation { get; set; }
    public List<ReserveProductDto> Products { get; set; } = [];
}
