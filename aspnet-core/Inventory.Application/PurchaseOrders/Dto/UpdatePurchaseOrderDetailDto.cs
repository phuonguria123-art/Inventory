using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto
{
    public sealed class UpdatePurchaseOrderDetailDto
    {
        public Guid? Id { get; set; }

        public Guid ProductId { get; set; }
        public int OrderedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Type { get; set; }
        public string? Note { get; set; }
    }
}
