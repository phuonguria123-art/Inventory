using AutoMapper;
using Inventory.Application.Interfaces;
using Inventory.Application.Users.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Inventory.Application.Users.Services
{
    public class AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper _mapper
        ) : IAuthService
    {
        public async Task<UserProfileDto?> RegisterAsync(RegisterRequestDto request)
        {
            var username = request.Username.Trim();
            var email = request.Email.Trim().ToLowerInvariant();
            ValidateUser(username, request.Password);
            if (string.IsNullOrWhiteSpace(email)) throw new ValidationException("Email không được bỏ trống");
            if (await userRepository.ExistByEmail(email))
            {
                throw new ConflictException("Email đã được sử dụng");
            }
            if (await userRepository.ExistByNameAsync(username))
                throw new ConflictException("Tên người dùng đã được sử dụng");

            var user = new User { PasswordHash = string.Empty, Email = email };
            string hashedPassword = BC.HashPassword(request.Password);
            user.Username = username;
            user.PasswordHash = hashedPassword;

            var roleStaff = await roleRepository.GetByNameAsync(nameof(RoleName.Staff));
            if (roleStaff == null)
            {
                throw new InvalidOperationException("role staff chưa được cấu hình");
            }
            user.UserRoles = [new UserRole { RoleId = roleStaff.Id, Role = roleStaff }];
            await userRepository.CreateAsync(user);
            return _mapper.Map<UserProfileDto>(user);
        }
        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var username = request.Username.Trim();
            ValidateUser(username, request.Password);
            var user = await userRepository.GetUserAsync(username);
            if (user is null)
            {
                return null;
            }
            if (!BC.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            return await CreateTokenResponse(user);
        }

        private void ValidateUser(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ValidationException("Tên người dùng không được bỏ trống");
            if (string.IsNullOrWhiteSpace(password)) throw new ValidationException("Mật khẩu không được bỏ trống");
        }
        public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
                return null;

            return await CreateTokenResponse(user);
        }
        private async Task<TokenResponseDto> CreateTokenResponse(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = jwtTokenGenerator.CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }
        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await userRepository.GetUserByIdAsync(userId);
            if (user is null ||
     user.RefreshToken != refreshToken ||
     user.RefreshTokenExpiryTime is null ||
     user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            return user;
        }
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await userRepository.UpdateAsync(user);
            return refreshToken;
        }
    }
}
