using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entity
{
    public class Product
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? ImgUrl { get; set; }
        public string? Origin { get; set; }
        public double Weight { get; set; }
        public string? LotNumber { get; set; }
        public string? BrandId { get; set; }
        public string? CategoryName { get; set; }
        public string? Size { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }

    }
}
