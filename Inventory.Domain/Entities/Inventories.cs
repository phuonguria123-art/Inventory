using System;

using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Inventories
    {
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid ProductId { get; set; }

        public int MinStock { get; set; }
        public int MaxStock { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }

        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        [NotMapped]
        public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public ICollection<InventoryReservation> Reservations { get; set; } = [];
        public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    }
}
