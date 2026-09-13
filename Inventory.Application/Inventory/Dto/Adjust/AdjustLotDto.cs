using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Adjust
{
    public sealed class AdjustLotDto
    {
        public string BatchNumber { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int ActualQuantity { get; set; }

    }
}
