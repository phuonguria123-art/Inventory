using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.InventoryItem
{
    public class InventoryItemCreateDto
    {
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; } = null!;
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
