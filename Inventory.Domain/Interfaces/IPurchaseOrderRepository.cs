using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IPurchaseOrderRepository
    {
        Task<PurchanseOrder?> GetAsync(Guid id);
        Task<List<PurchanseOrder>> GetListAsync();
        Task CreateAsync(PurchanseOrder purchanseOrder);
        Task UpdateAsync(PurchanseOrder purchanseOrder);
    }
}
