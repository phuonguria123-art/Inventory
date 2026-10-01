using Inventory.Application.PurrchanseOrderDetail.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Dto
{
    public class CreatePurchaseOrderDto
    {
        public required Guid SupplierId { set; get; }
        /// <summary>
        /// Ngày dự kiến nhận
        /// </summary>
        public DateTime ExpectedReceiveDate { get; set; }
        /// <summary>
        /// Địa điểm nhận (có nhiều kho thì chọn kho nhận ) có thể đặt tên là ReceivingLocation
        /// </summary>
        public required Guid ReceivingWarehouseId { get; set; }
        public decimal? Freight { get; set; }
        public string? Type { get; set; }
        /// <summary>
        /// tổng cuối(sau khi đã cộng các phí phụ)
        /// </summary>
        public decimal? TotalWeight { get; set; }
        public List<PurchaseOrderDetailDto> Products { get; set; } = new();
    }
}
