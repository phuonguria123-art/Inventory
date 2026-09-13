using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Application.Inventory.Dto.Transfer;

public sealed class TransferLotDto
{
    public string BatchNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [JsonPropertyName("location")]
    public string? DestinationLocation { get; set; }
}
