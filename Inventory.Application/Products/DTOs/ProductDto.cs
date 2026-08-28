using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Products.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? ImgUrl { get; set; }
        public string? Origin { get; set; }
        public double Weight { get; set; }
        public Guid? SupplierId { get; set; }
    }
}
