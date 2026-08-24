using Inventory.Application.Users.DTOs;
using Inventory.Application.Users.Services;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        IAuthService authService) : ControllerBase
    {
        public static User user = new() { PasswordHash = string.Empty, Username = string.Empty };
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
           var user = await authService.RegisterAsync(request);
            if (user == null) {
                return BadRequest("Tên người dùng đã tồn tại");
            }
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            var token = await authService.LoginAsync(request);
            if (token == null)
            {
                return BadRequest("Tên đăng nhập hoặc mật khẩu không hợp lệ");
            }
            return Ok(token);
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
                return Unauthorized("Invalid refresh token.");

            return Ok(result);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated!");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are and admin!");
        }
    }
}
