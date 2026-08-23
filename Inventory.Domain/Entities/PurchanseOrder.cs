using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class PurchanseOrder
    {
        public Guid Id { get; set; }
        public required string Code { get; set; }
        public required Guid SupplierId { set; get; }
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
        public DateTime? ReiceiveDate { get; set; }
        /// <summary>
        /// Nhân viên nhận
        /// </summary>
        public string? ReceivedTo { get; set; }
        /// <summary>
        /// Địa điểm nhận (có nhiều kho thì chọn kho nhận ) có thể đặt tên là ReceivingLocation
        /// </summary>
        public required Guid ReceivingWarehouseId { get; set; }
        public decimal? Freight { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        /// <summary>
        /// tổng cuối(sau khi đã cộng các phí phụ)
        /// </summary>
        public decimal? TotalPrice { get; set; }
        public string? TotalWeight { get; set; }
        public string? Type { get; set; }
        public virtual ICollection<PurchanseOrderDetail> OrderDetails { get; set; } = new List<PurchanseOrderDetail>();
    }
}
