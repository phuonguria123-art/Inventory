using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IPurchaseOrderRepository
    {
        Task<PurchaseOrder?> GetAsync(Guid id);

        Task<PurchaseOrder?> GetWithDetailsAsync(Guid id);
        Task<List<PurchaseOrder>> GetListAsync();
        Task<(List<PurchaseOrder> items, int totalCount)> GetPageAsync(int pageSize, int pageNumber, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, DateTime? createDate);
        Task CreateAsync(PurchaseOrder purchaseOrder);
        Task UpdateAsync(PurchaseOrder purchaseOrder);
        Task SaveChangesAsync();
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
