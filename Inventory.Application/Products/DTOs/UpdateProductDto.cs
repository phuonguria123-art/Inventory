using System;

namespace Inventory.Application.Products.DTOs
{
    public class UpdateProductDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? ImgUrl { get; set; }
        public string? Origin { get; set; }
        public double Weight { get; set; }
        public Guid? SupplierId { get; set; }
    }
}
