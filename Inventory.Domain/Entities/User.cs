using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entitys
{
    public class User
    {
        public Guid Id {  get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
