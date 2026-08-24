using System;

namespace Inventory.Domain.Entities
{
    //Theo dõi hàng tồn vật lý với thông tin lô/hết hạn
    public class InventoryItem
    {
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; } = null!;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime DateReceived { get; set; }
        public string Location { get; set; } = null!;

        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
