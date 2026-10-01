using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Warehouses.DTOs
{
    public class CreateWarehouseDto
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Phone { get; set; }
        public required string Address { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public bool IsMain { get; set; }
    }
}
