using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public class InventoryTransactionDto
    {
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? InventoryItemId { get; set; }

        public InventoryTransactionType TransactionType { get; set; }
        public int Quantity { get; set; }
        public string Reference { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid CreatedByUserId { get; set; }
    }
}
