using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public class Permission
    {
        public Guid Id { get; set; }

        public required string  Code { get; set; }
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = [];

    }
}
