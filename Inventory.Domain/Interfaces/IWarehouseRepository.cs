using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse> GetAsync(Guid id);
        Task<List<Warehouse>> GetAllAsync();
        Task CreateAysnc(Warehouse warehouse);
        Task UpdateAsync(Warehouse warehouse);
        Task DeleteAsync(Warehouse warehouse);
        Task<bool> ExistsAsync(Guid id);
    }
}
