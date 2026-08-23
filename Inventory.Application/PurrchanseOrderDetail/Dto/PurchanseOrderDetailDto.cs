using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurrchanseOrderDetail.Dto
{
    public class PurchanseOrderDetailDto
    {
        public string PurchanseOrderId { get; set; }
        public Guid ProductId { get; set; }
        /// <summary>
        /// Số lượng đã đặt
        /// </summary>
        public int OrderedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Note { get; set; }
    }
}
