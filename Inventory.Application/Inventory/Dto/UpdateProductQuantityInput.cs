using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public class UpdateProductQuantityInput
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public int QUantity { get; set; }
    }
}
