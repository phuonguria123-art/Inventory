using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryRepository
    {
        Task<Inventories?> GetAsync(Guid warehouseId, Guid productId);
        Task<Inventories?> GetById(Guid inventoryId);
        Task<Inventories?> GetReadOnlyAsync(Guid warehouseId, Guid productId);
        Task<(List<Inventories> listInventory, int totalCount)> GetAllAsync(
            int pageSize,
            int pageNumber,
            Guid? warehouseId,
            Guid? productId,
            string? warehouseSearch,
            string? productSearch,
            string? sortBy,
            bool sortDescending);
        Task<(List<InventoryTransaction> listInventoryTransaction, int totalCount)> GetAllTransactionAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference);
        Task<List<InventoryReservation>> GetAllReservationExpired();
        Task<bool> TransactionReferenceExistsAsync(Guid warehouseId, InventoryTransactionType transactionType, string reference);
        Task<InventoryReservation?> GetActiveReservationAsync(Guid inventoryId, string reference);
        Task<List<Inventories>> GetLowOnStockAsync();
        Task<List<Inventories>> GetExcessGoodsAsync();
        Task<InventoryItem?> GetInventoryItemAsync(Guid inventoryId, string batchNumber);
        Task AddAsync(Inventories inventory);
        Task AddTransactionAsync(InventoryTransaction transaction);
        Task AddReservationAsync(InventoryReservation reservation);
        Task AddInventoryItem(InventoryItem item);
        Task UpdateAsync(Inventories inventory);
        Task UpdateRevationAsync(InventoryReservation reservation);
        Task UpdateInventoryItem(InventoryItem inventoryItem);
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
