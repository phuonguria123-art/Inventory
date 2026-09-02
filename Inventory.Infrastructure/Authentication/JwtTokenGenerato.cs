using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Authentication
{
    public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
    {
        public string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var roles = user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct();

            claims.AddRange(
                roles.Select(role =>
                    new Claim(ClaimTypes.Role, role)));

            var permissions = user.UserRoles
                .SelectMany(userRole => userRole.Role.RolePermissions)
                .Select(rolePermission => rolePermission.Permission.Code)
                .Distinct();

            claims.AddRange(
                permissions.Select(permission =>
                    new Claim("permission", permission)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["AppSettings:Token"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration["AppSettings:Issuer"],
                audience: configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes()),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private int GetAccessTokenMinutes()
        {
            return int.TryParse(configuration["AppSettings:AccessTokenMinutes"], out var minutes)
                && minutes > 0
                    ? minutes
                    : 15;
        }

    }
}
