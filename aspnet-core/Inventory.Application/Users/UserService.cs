using AutoMapper;
using Inventory.Application.Users.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
        }
        public async Task<List<UserProfileDto>> GetAllUser()
        {
            var users = await _userRepository.GetListUser();
            return _mapper.Map<List<UserProfileDto>>(users);
        }
        public async Task<UserProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is null)
                throw new NotFoundException("Không tìm thấy người dùng.");

            return _mapper.Map<UserProfileDto>(user);
        }
        public async Task<List<RoleDto>> GetAllAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            var result = roles.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,

                Permissions = role.RolePermissions
                   .Select(x => new PermissionDto
                   {
                       Id = x.Permission.Id,
                       Code = x.Permission.Code,
                       Description = x.Permission.Description
                   })
                   .ToList()
            }).ToList();
            return result;
        }

        public async Task UpdateUserRolesAsync(Guid userId, List<Guid> roleIds)
        {
            var distinctRoleIds = roleIds.Distinct().ToList();
            if (distinctRoleIds.Count == 0)
                throw new ValidationException("Người dùng phải có ít nhất một vai trò.");

            var roles = await _roleRepository.GetByIdsAsync(distinctRoleIds);
            if (roles.Count != distinctRoleIds.Count)
                throw new ValidationException("Danh sách vai trò chứa giá trị không hợp lệ.");

            var user = await _userRepository.GetUserWithRolesAsync(userId);

            if (user is null)
                throw new NotFoundException("Không tìm thấy người dùng.");
            var rolesToRemove = user.UserRoles
                .Where(userRole => !distinctRoleIds.Contains(userRole.RoleId))
                .ToList();
            foreach (var role in rolesToRemove)
                user.UserRoles.Remove(role);

            var existingRoleIds = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
            foreach (var role in roles.Where(role => !existingRoleIds.Contains(role.Id)))
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    Role = role
                });
            }

            await _userRepository.UpdateAsync(user);
        }
    }
}
