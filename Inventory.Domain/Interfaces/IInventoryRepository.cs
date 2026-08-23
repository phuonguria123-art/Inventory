using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryRepository
    {
        Task<Inventories> GetAsync(Guid warehouseid, Guid ProductId);
        Task<List<Inventories>> GetAllAsync();
        Task CreateAsync(Inventories inventories);
        Task UpdateAsync(Inventories inventories);
        Task DeleteAsync(Inventories inventories);
    }
}
