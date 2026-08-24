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
        Task<Boolean> GetUserByUsername(string username);
        Task<User> GetUser(string username);
        Task<User> GetUserById(Guid id);
        Task Create(User user);
        Task Update(User user);
    }
}
