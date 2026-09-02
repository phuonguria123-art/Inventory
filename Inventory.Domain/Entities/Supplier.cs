using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class Supplier
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<Product> Products { get; set; } = [];
    }
}
