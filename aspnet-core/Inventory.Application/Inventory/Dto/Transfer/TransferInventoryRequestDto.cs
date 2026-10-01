using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Application.Inventory.Dto.Transfer;

public sealed class TransferInventoryRequestDto
{
    [JsonPropertyName("warehouseFromId")]
    public Guid SourceWarehouseId { get; set; }

    [JsonPropertyName("warehouseToId")]
    public Guid DestinationWarehouseId { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<TransferProductDto> Products { get; set; } = [];
}
