using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Inventory.Dto.Issue;

public sealed class IssueLotDto
{
    [Required]
    [MaxLength(100)]
    public string BatchNumber { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
