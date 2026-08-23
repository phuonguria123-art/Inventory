using Inventory.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class Inventories

    {
        public Guid Id { get; set; }
        /// <summary>
        /// kho chứa sản phẩm đó
        /// </summary>
        public Guid WarehouseId { get; set; }
        public Guid ProductId { get; set; }
        /// <summary>
        /// số lượng tồn tối thiểu để cảnh báo
        /// </summary>
        public int MinStock { get; set; }
        /// <summary>
        /// Số lượng tồn tối đa nhập >= QuantityOnHand
        /// </summary>
        public int MaxStock { get; set; }
        public string? Description { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Status { get; set; }
        /// <summary>
        /// Số lượng hiện có(bao gồm cả số có sẵn trong kho và số lương đã đặt nhưng chưa nhận)
        /// </summary>
        public int QuantityOnHand { get; set; }
        /// <summary>
        /// Số lượng đã đặt trước
        /// </summary>
        public int ReservedQuantity { get; set; }
        /// <summary>
        /// số lượng có sẵn
        /// </summary>
        public int AvailableQuantity { get; set; }
        public DateTime LastUpdate { get; set; }
        public virtual Warehouse Warehouse { get; set; }

        public virtual Product Product { get; set; }
    }
}
