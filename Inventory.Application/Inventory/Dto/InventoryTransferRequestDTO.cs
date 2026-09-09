using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public sealed class InventoryTransferRequestDto
    {
        public Guid WarehouseFromId { get; set; }
        public Guid WarehouseToId { get;set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        public Guid ProductId { get; set; }
        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
