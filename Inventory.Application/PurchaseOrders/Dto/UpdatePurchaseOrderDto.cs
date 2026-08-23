using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto
{
    public class UpdatePurchaseOrderDto
    {
        public Guid Id { get; set; }
        public string? ReceivedTo { get; set; }
        public List<PurchanseOrderDetail> OrderDetails { get; set; } = new();
    }
}
