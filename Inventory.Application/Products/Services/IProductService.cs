using Inventory.Application.Products.DTOs;
using Inventory.Application.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Products.Services
{
    public interface IProductService
    {
        Task AddAsync(CreateProductDto product);
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto> GetByIdAsync(Guid id);
        Task UpdateAsync(UpdateProductDto product);
        Task DeleteAsync(Guid id);
    }
}
