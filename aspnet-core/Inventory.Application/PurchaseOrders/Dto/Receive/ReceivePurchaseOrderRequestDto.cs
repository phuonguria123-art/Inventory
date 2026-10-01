using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto.Receive
{
    public sealed class ReceivePurchaseOrderRequestDto
    {
        public Guid PurchaseOrderId { get; set; }
        public List<ReceivePurchaseOrderProductDto> OrderDetails { get; set; } = [];
    }
}
