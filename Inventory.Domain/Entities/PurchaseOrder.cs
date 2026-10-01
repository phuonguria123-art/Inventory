using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class PurchaseOrder
    {
        public Guid Id { get; set; }
        public required string Code { get; set; }
        public required Guid SupplierId { get; set; }
        //ngày tạo đơn 
        public DateTime CreateDate { get; set; }
        //ngày gửi đơn tới Ncc 
        public DateTime? SendDate { get; set; }
        /// <summary>
        /// Ngày dự kiến nhận
        /// </summary>
        public DateTime ExpectedReceiveDate { get; set; }
        /// <summary>
        /// Ngày nhận thực tế
        /// </summary>
        public DateTime? ReceivedDate { get; set; }
        /// <summary>
        /// Nhân viên nhận
        /// </summary>
        public string? ReceivedTo { get; set; }
        /// <summary>
        /// Địa điểm nhận có thể đặt tên là ReceivingLocation
        /// </summary>
        public required Guid ReceivingWarehouseId { get; set; }
        public decimal? Freight { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        /// <summary>
        /// tổng cuối(sau khi đã cộng các phí phụ)
        /// </summary>
        public decimal? TotalPrice { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? Type { get; set; }

        public virtual ICollection<PurchaseOrderDetail> OrderDetails { get; set; } = new List<PurchaseOrderDetail>();
        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
