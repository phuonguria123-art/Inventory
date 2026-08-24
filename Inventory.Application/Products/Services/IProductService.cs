using Inventory.Application.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Application.Products.Services
{
    public interface IProductService
    {
        Task<ProductDto> CreateAsync(CreateProductDto product);
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto> GetByIdAsync(Guid id);
        Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto product);
        Task DeleteAsync(Guid id);
    }
}
