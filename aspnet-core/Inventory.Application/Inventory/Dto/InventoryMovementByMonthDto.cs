using Inventory.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public sealed class InventoryMovementByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalStockIn { get; set; }
        public int TotalStockOut { get; set; }
    }

    public sealed class InventoryMovementSummaryDto
    {
        public Guid ProductId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<InventoryMovementByMonthDto> Items { get; set; } = [];
    }
}
