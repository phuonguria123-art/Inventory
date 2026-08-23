using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users.DTOs
{
    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; }
    }
}
