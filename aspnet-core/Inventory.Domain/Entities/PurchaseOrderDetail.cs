using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class PurchaseOrderDetail
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid ProductId { get; set; }

        public int OrderedQuantity { get; set; }
        public int ActualReceivedQuantity { get; set; }
        public string? Type { get; set; } 

        public DateTime? ReceiveTime { get; set; }
        public string? InventoryId { get; set; } 
        public decimal UnitPrice { get; set; }
        public string Note { get; set; } = string.Empty;

        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
