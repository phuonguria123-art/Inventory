using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Models
{
    public sealed class InventoryMovementMonthlyResult
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalStockIn { get; set; }
        public int TotalStockOut { get; set; }
    }
}
