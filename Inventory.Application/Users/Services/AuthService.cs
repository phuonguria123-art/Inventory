using AutoMapper;
using Inventory.Application.Interfaces;
using Inventory.Application.Users.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Inventory.Application.Users.Services
{
    public class AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator
        ) : IAuthService
    {
        public async Task<TokenResponseDto?> LoginAsync(UserDto request)
        {
            var user = await userRepository.GetUser(request.Username);
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
        public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
                return null;

            return await CreateTokenResponse(user);
        }
        private async Task<TokenResponseDto> CreateTokenResponse(User? user)
        {
            return new TokenResponseDto
            {
                AccessToken = jwtTokenGenerator.CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        public async Task<User?> RegisterAsync(UserDto request)
        {
            if (await userRepository.GetUserByUsername(request.Username))
                return null;

            var user = new User { PasswordHash = string.Empty };
            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, request.Password);
            user.Username = request.Username;
            user.PasswordHash = hashedPassword;
            await userRepository.Create(user);
            return user;
        }
        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await userRepository.GetUserById(userId);
            if (user is null || user.RefreshToken != refreshToken
                || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
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

            await userRepository.Update(user);
            return refreshToken;
        }
    }
}
