using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public class InventoryCheckDto
    {
        public Guid WarehouseId { get; set; }

        public List<InventoryCheckItemDto> Products { get; set; } = new();
    }

    public class InventoryCheckItemDto
    {
        public Guid ProductId { get; set; }
        public int ActualQuantity { get; set; }
    }
}
