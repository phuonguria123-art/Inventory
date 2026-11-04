using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto
{
    public sealed class UpdateStockLevelsRequest
    {
        public int MinStock { get; set; }
        public int MaxStock { get; set; }
    }
}
