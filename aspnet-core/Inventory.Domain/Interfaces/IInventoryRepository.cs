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
        Task<InventoryBalance?> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId);
        Task<InventoryBalance?> GetByIdAsync(Guid inventoryId);
        Task<InventoryBalance?> GetReadOnlyByWarehouseAndProductAsync(Guid warehouseId, Guid productId);
        Task<(List<InventoryBalance> Items, int TotalCount)> GetPagedAsync(
            int pageSize,
            int pageNumber,
            Guid? warehouseId,
            Guid? productId,
            string? warehouseSearch,
            string? productSearch,
            string? sortBy,
            bool sortDescending);
        Task<(List<InventoryTransaction> Items, int TotalCount)> GetTransactionsAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference);
        Task<List<InventoryReservation>> GetExpiredActiveReservationsAsync();
        Task<bool> TransactionReferenceExistsAsync(Guid warehouseId, InventoryTransactionType transactionType, string reference);
        Task<InventoryReservation?> GetActiveReservationAsync(Guid inventoryId, string reference);
        Task<List<InventoryBalance>> GetLowStockAsync();
        Task<List<InventoryBalance>> GetExcessStockAsync();
        Task<InventoryItem?> GetInventoryItemAsync(Guid inventoryId, string batchNumber);
        Task AddInventoryAsync(InventoryBalance inventory);
        Task AddTransactionAsync(InventoryTransaction transaction);
        Task AddReservationAsync(InventoryReservation reservation);
        Task AddInventoryItemAsync(InventoryItem item);
        Task UpdateInventoryAsync(InventoryBalance inventory);
        Task UpdateReservationAsync(InventoryReservation reservation);
        Task UpdateInventoryItemAsync(InventoryItem inventoryItem);
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
