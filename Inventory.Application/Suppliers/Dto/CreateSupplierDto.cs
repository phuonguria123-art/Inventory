using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.Suppliers.Dto
{
    public class CreateSupplierDto
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
        [EmailAddress]
        public required string Email { get; set; }
        public required string Code { get; set; }
        public required string ContactPerson { get; set; }
    }
}
