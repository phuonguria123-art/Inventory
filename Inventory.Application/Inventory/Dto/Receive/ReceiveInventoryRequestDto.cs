using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Receive
{
    public sealed class ReceiveInventoryRequestDto
    {
        public Guid WarehouseId { get; set; }

        // Có khi nhận hàng từ đơn mua
        public Guid? PurchaseOrderId { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public List<ReceiveProductDto> Products { get; set; } = [];
    }
}
