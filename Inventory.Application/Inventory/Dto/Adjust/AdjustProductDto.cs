using Inventory.Application.Inventory.Dto.Receive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Adjust
{
    public sealed class AdjustProductDto
    {
        public Guid ProductId { get; set; }

        public List<AdjustLotDto> Lots { get; set; } = [];
    }
}
