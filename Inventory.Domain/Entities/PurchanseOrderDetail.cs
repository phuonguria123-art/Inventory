using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class PurchanseOrderDetail
    {
        public Guid Id { get; set; }
        public Guid PurchanseOrderId { get; set; }
        public Guid ProductId { get; set; }
        /// <summary>
        /// Số lượng đã đặt
        /// </summary>
        public int OrderedQuantity { get; set; }
        /// <summary>
        /// Số lượng thực tế nhận
        /// </summary>
        public int ActualReceivedQuantity { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// số lô này có thể mở rộng sau
        /// </summary>
        //public string LotNumber { get; set; }

        //ngày nhận
        public DateTime ReceiveTime { get; set; }
        /// <summary>
        /// phiếu tồn khi nhập hàng
        /// </summary>
        public string InventoryId { get; set; }
        public decimal UnitPrice { get; set; }
        public string Note { get; set; }
    }
}
