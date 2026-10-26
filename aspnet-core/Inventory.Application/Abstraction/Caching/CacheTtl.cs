using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Abstraction.Caching
{
    public static class CacheTtl
    {
        public static readonly TimeSpan Inventory = TimeSpan.FromMinutes(5);
    }
}
