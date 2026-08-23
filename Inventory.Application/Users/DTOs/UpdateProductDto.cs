using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Users.DTOs
{
    public class UpdateProductDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? ImgUrl { get; set; }
        public string? Origin { get; set; }
        public double Weight { get; set; }
    }
}
