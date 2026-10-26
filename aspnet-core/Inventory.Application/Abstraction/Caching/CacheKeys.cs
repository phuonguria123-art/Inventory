using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Abstraction.Caching
{
    public static class CacheKeys
    {
        public static class Inventory
        {
            public const string LowStock = "inventory:low-stock:v1";
            public const string ExcessStock = "inventory:excess-stock:v1";
        }
    }
}
