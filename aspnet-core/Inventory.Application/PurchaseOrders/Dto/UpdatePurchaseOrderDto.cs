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
        public required Guid SupplierId { set; get; }
        public DateTime ExpectedReceiveDate { get; set; }
        public required Guid ReceivingWarehouseId { get; set; }
        public decimal? Freight { get; set; }
        public string? Type { get; set; }
        public decimal? TotalWeight { get; set; }
        public List<UpdatePurchaseOrderDetailDto> OrderDetails { get; set; } = new();
    }
}
