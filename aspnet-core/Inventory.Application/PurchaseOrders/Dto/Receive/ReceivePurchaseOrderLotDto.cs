using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto.Receive
{
    public sealed class ReceivePurchaseOrderLotDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public int ReceivedQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Location { get; set; }
    }
}
