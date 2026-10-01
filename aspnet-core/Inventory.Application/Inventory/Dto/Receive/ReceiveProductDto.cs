using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Receive
{
    public sealed class ReceiveProductDto
    {
        public Guid ProductId { get; set; }

        public List<ReceiveLotDto> Lots { get; set; } = [];
    }
}
