using Inventory.Application.Authorization;
using Inventory.Application.Users;
using Inventory.Application.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Inventory.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        //Quản lý tài khoản và gán role cho user
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        [Authorize(Policy = PermissionCodes.UserRead)]
        [HttpGet]
        public async Task<IActionResult> GetListUser()
        {
            var users = await _userService.GetAllUser();
            return Ok(users);
        }

        [Authorize(Policy = PermissionCodes.RoleRead)]
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoleAll()
        {
            var listUser = await _userService.GetAllAsync();
            return Ok(listUser);
        }
        [Authorize(Policy = PermissionCodes.RoleManage)]
        [HttpPut("{userId:guid}/roles")]

        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] List<Guid> roleIds)
        {
            await _userService.UpdateUserRolesAsync(userId, roleIds);
            return NoContent();
        }

    }
}
