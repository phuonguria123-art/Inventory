using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{ 
    public sealed class IdempotencyRecord
    {
        public Guid Id { get; set; }

        public required string Key { get; set; }

        public required string Operation { get; set; }

        public Guid ResourceId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = "Completed";
    }
}
