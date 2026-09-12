using Inventory.Application.Inventory.Dto.Receive;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Dto.Issue
{
    public class IssueInventoryRequestDto
    {
        public Guid WarehouseId { get; set; }
        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
        public List<IssueProductDto> Products { get; set; } = [];
    }
}
