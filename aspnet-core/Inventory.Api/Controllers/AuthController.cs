using Inventory.Application.Users.DTOs;
using Inventory.Application.Users.Services;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        IAuthService authService,
        Inventory.Application.Users.IUserService userService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterRequestDto request)
        {
            var user = await authService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created, user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request)
        {
            var token = await authService.LoginAsync(request);
            if (token == null)
            {
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không hợp lệ.");
            }
            return Ok(token);
        }
        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
                return Unauthorized("Invalid refresh token.");
            return Ok(result);
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            return Ok(await userService.GetProfileAsync(userId));
        }
        //change-password

    }
}
