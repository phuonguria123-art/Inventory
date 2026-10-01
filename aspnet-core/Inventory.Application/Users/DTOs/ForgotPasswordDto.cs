using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users.DTOs
{
    public class ForgotPasswordDto
    {
        public required string Username { get; set; }
        public required string PasswordOld { get; set; }
        public required string PasswordNew { get; set; }
    }
}
