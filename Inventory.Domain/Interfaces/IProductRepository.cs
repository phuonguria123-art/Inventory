using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);

        Task<List<Product>> GetAllAsync();
        Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task<List<Guid>> NotExits(List<Guid> ids);
        Task<bool> ExistByIdAsync(Guid Id);
        Task<bool> ExistByCodeAsync(string productCode, Guid? excludeId = null);
    }
}
