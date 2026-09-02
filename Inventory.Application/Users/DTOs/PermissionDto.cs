using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users.DTOs
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
    }
}
