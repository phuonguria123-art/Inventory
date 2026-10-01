using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto.Receive
{
    public sealed class ReceivePurchaseOrderProductDto
    {
        public Guid PurchaseOrderDetailId { get; set; }
        public List<ReceivePurchaseOrderLotDto> Lots { get; set; } = [];
    }
}
