using System;

namespace Inventory.Domain.Entities
{
    //Theo dõi hàng tồn vật lý với thông tin lô/hết hạn
    public sealed class InventoryItem
    {
        public Guid Id { get; set; }
        public Guid InventoryId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime DateReceived { get; set; }
        public string Location { get; set; } = string.Empty;
        public InventoryBalance Inventory { get; set; } = null!;
    }
}
