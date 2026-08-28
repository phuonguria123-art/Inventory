using AutoMapper;
using Inventory.Application.Interfaces;
using Inventory.Application.Users.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Inventory.Application.Users.Services
{
    public class AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper _mapper
        ) : IAuthService
    {
        public async Task<UserProfileDto?> RegisterAsync(RegisterRequestDto request)
        {
            ValidateUser(request.Username, request.Password);
            if (string.IsNullOrWhiteSpace(request.Email)) throw new ValidationException("Email không được bỏ trống");

            if (await userRepository.ExistByNameAsync(request.Username))
                throw new ConflictException("Tên người dùng đã được sử dụng");

            var user = new User { PasswordHash = string.Empty, Email = request.Email };
            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, request.Password);
            user.Username = request.Username;
            user.PasswordHash = hashedPassword;
            await userRepository.CreateAsync(user);
            return _mapper.Map<UserProfileDto>(user);
        }
        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto request)
        {
            ValidateUser(request.Username, request.Password);
            var user = await userRepository.GetUserAsync(request.Username);
            if (user is null)
            {
                return null;
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
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
        public async Task UpdateUserRolesAsync(
    Guid userId,
    IReadOnlyCollection<Guid> roleIds)
        {
            var user = await userRepository.GetUserWithRolesAsync(userId);

            if (user is null)
                throw new NotFoundException("Không tìm thấy người dùng.");

            user.UserRoles.Clear();

            foreach (var roleId in roleIds.Distinct())
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                });
            }

            await userRepository.UpdateAsync(user);
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
