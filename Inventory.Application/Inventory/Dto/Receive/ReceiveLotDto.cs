using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Receive
{
    public sealed class ReceiveLotDto
    {
        public string BatchNumber { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }

        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string? Location { get; set; }
    }
}
