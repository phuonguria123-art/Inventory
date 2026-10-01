using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string CreateToken(User user);
        string GenerateRefreshToken();
    }
}
