using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public class InventoryItemDto
    {
        public Guid Id { get; set; }
        public Guid InventoryId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime DateReceived { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
