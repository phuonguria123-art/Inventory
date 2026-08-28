using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class UserRoleRepositoty(ApplicationDbContext _context) : IUserRoleRepositoty
    {
    }
}
