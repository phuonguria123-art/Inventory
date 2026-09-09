using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class InventoryReservation
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Active;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public Inventories Inventory { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
