using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Suppliers.Dto
{
    public  class SupplierDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string Code { get; set; }
        public required string ContactPerson { get; set; }
        public bool IsActive { get; set; }
    }
}
