using Inventory.Application.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Application.Products.Services
{
    public interface IProductService
    {
        Task<ProductDto> CreateAsync(CreateProductDto product);
        Task<global::Inventory.Application.Common.Models.PagedResult<ProductDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20);
        Task<ProductDto> GetByIdAsync(Guid id);
        Task<ProductDto> UpdateAsync(UpdateProductDto product);
        Task DeleteAsync(Guid id);
    }
}
