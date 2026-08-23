using Inventory.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interface
{
    public interface IProductRepository
    {
        Task<Product> GetByIdAsync(Guid id);

        Task<List<Product>> GetAllAsync();

        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task<List<Guid>> NotExits(List<Guid> ids);
    }
}
