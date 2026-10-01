using Inventory.Application.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users
{
    public interface IUserService
    {
        Task<List<UserProfileDto>> GetAllUser();
        Task<UserProfileDto> GetProfileAsync(Guid userId);
        Task<List<RoleDto>> GetAllAsync();
        Task UpdateUserRolesAsync(Guid userId,List<Guid> roleIds);
    }
}
