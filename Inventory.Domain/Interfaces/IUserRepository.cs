using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistByNameAsync(string username);
        Task<User?> GetUserAsync(string username);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserWithRolesAsync(Guid userId);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
    }
}
