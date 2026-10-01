using System;

using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities
{
    //Lịch sử audit cho chuyển động kho
    public class InventoryTransaction
    {
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? InventoryItemId { get; set; }

        public InventoryTransactionType TransactionType { get; set; }
        public int Quantity { get; set; }
        public string Reference { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid CreatedByUserId { get; set; }

        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual InventoryItem? InventoryItem { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;
    }
}
