using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class UserRepository(ApplicationDbContext _context) : IUserRepository
    {
        public async Task CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
        public async Task<User?> GetUserAsync(string username)
        {
            return await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.Username == username.Trim());
        }
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users
       .Include(x => x.UserRoles)
           .ThenInclude(x => x.Role)
               .ThenInclude(x => x.RolePermissions)
                   .ThenInclude(x => x.Permission)
       .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<bool> ExistByNameAsync(string username)
        {
            return _context.Users.AnyAsync(x => x.Username == username.Trim());
        }
        public Task<bool> ExistByEmail(string email)
        { return _context.Users.AnyAsync(x => x.Email == email.Trim().ToLower()); }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task<User?> GetUserWithRolesAsync(Guid userId)
        {
            return await _context.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(user => user.Id == userId);
        }
        public async Task<List<User>> GetListUser()
        {
            return await _context.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ToListAsync();
        }

    }
}
