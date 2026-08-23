using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface ISupplierRepository
    {
        Task<Supplier?> GetAsync(Guid id);

        Task<List<Supplier>> GetAllAsycn();

        Task CreateAsync(Supplier supplier);

        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(Supplier supplier);
        Task<bool> ExistsAsync(Guid supplierId);
    }
}
