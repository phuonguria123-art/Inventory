using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class UserRoleRepository(ApplicationDbContext context) : IUserRoleRepository
    {
        public async Task CreateAsync(UserRole userRole)
        {
           context.UserRoles.Add(userRole);
           await context.SaveChangesAsync();
        }
    }
}