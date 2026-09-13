using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class InventoryRepository(ApplicationDbContext _context) : IInventoryRepository
    {
        public async Task AddInventoryAsync(InventoryBalance inventory)
        {
            await _context.InventoryBalances.AddAsync(inventory);
        }

        public async Task AddTransactionAsync(InventoryTransaction transaction)
        {
            await _context.InventoryTransactions.AddAsync(transaction);
        }

        public async Task AddReservationAsync(InventoryReservation reservation)
        {
            await _context.InventoryReservations.AddAsync(reservation);
        }

        public async Task AddInventoryItemAsync(InventoryItem item)
        {
            await _context.InventoryItems.AddAsync(item);
        }
        public async Task<(List<InventoryBalance> Items, int TotalCount)> GetPagedAsync(
            int pageSize,
            int pageNumber,
            Guid? warehouseId,
            Guid? productId,
            string? warehouseSearch,
            string? productSearch,
            string? sortBy,
            bool sortDescending)
        {
            var query = _context.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            if (!string.IsNullOrWhiteSpace(warehouseSearch))
            {
                var keyword = warehouseSearch.Trim();
                query = query.Where(x =>
                    x.Warehouse.Code.Contains(keyword) ||
                    x.Warehouse.Name.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(productSearch))
            {
                var keyword = productSearch.Trim();
                query = query.Where(x =>
                    x.Product.Code.Contains(keyword) ||
                    x.Product.Name.Contains(keyword));
            }

            int totalCount = await query.CountAsync();
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
            var orderedQuery = normalizedSortBy switch
            {
                "availablequantity" => sortDescending
                    ? query.OrderByDescending(x => x.QuantityOnHand - x.ReservedQuantity)
                    : query.OrderBy(x => x.QuantityOnHand - x.ReservedQuantity),
                "quantityonhand" => sortDescending
                    ? query.OrderByDescending(x => x.QuantityOnHand)
                    : query.OrderBy(x => x.QuantityOnHand),
                "productname" => sortDescending
                    ? query.OrderByDescending(x => x.Product.Name)
                    : query.OrderBy(x => x.Product.Name),
                "warehousename" => sortDescending
                    ? query.OrderByDescending(x => x.Warehouse.Name)
                    : query.OrderBy(x => x.Warehouse.Name),
                _ => sortDescending
                    ? query.OrderByDescending(x => x.LastUpdatedAt)
                    : query.OrderBy(x => x.LastUpdatedAt)
            };

            var result = await orderedQuery
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (result, totalCount);
        }

        public async Task<InventoryBalance?> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId)
        {
            return await _context.InventoryBalances.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId && x.ProductId == productId);
        }
        public async Task<InventoryBalance?> GetByIdAsync(Guid inventoryId)
        {
            return await _context.InventoryBalances.FirstOrDefaultAsync(x => x.Id == inventoryId);
        }

        public async Task<InventoryBalance?> GetReadOnlyByWarehouseAndProductAsync(Guid warehouseId, Guid productId)
        {
            return await _context.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId && x.ProductId == productId);
        }
        public async Task<bool> TransactionReferenceExistsAsync(
            Guid warehouseId,
            InventoryTransactionType transactionType,
            string reference)
        {
            return await _context.InventoryTransactions.AnyAsync(transaction =>
                transaction.WarehouseId == warehouseId &&
                transaction.TransactionType == transactionType &&
                transaction.Reference == reference);
        }

        public async Task<InventoryReservation?> GetActiveReservationAsync(Guid inventoryId, string reference)
        {
            return await _context.InventoryReservations.FirstOrDefaultAsync(reservation =>
                reservation.InventoryId == inventoryId &&
                reservation.Reference == reference &&
                reservation.Status == InventoryReservationStatus.Active);
        }
        public async Task<List<InventoryReservation>> GetExpiredActiveReservationsAsync()
        {
            return await _context.InventoryReservations
    .Where(reservation =>
        reservation.Status == InventoryReservationStatus.Active &&
        reservation.ExpiresAt.HasValue &&
        reservation.ExpiresAt.Value <= DateTime.UtcNow)
    .ToListAsync();
        }
        public async Task<(List<InventoryTransaction> Items, int TotalCount)> GetTransactionsAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference)
        {
            var query = _context.InventoryTransactions.AsNoTracking();
            if (warehouseId.HasValue) { query = query.Where(x => x.WarehouseId == warehouseId); }
            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId);
            }
            if (transactionType.HasValue) { query = query.Where(x => x.TransactionType == transactionType); }
            if (createdByUserId.HasValue) { query = query.Where(x => x.CreatedByUserId == createdByUserId); }
            if (!string.IsNullOrWhiteSpace(reference)) { query = query.Where(x => x.Reference == reference); }

            int totalCount = await query.CountAsync();
            var transactions = await query
                  .OrderByDescending(x => x.TransactionDate)
                  .ThenByDescending(x => x.Id)
                  .Skip((pageNumber - 1) * pageSize)
                  .Take(pageSize)
                  .ToListAsync();
            return (transactions, totalCount);
        }
        public async Task<List<InventoryBalance>> GetLowStockAsync()
        {
            return await _context.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .Where(x => x.QuantityOnHand - x.ReservedQuantity <= x.MinStock)
                .OrderBy(x => x.QuantityOnHand - x.ReservedQuantity)
                .ThenBy(x => x.Product.Name)
                .ToListAsync();
        }
        public async Task<List<InventoryBalance>> GetExcessStockAsync()
        {
            return await _context.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .Where(x => x.QuantityOnHand > x.MaxStock)
                .OrderByDescending(x => x.QuantityOnHand - x.MaxStock)
                .ThenBy(x => x.Product.Name)
                .ToListAsync();
        }
        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            if (!_context.Database.IsRelational())
            {
                await operation();
                await _context.SaveChangesAsync();
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateInventoryAsync(InventoryBalance inventory)
        {
            _context.InventoryBalances.Update(inventory);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateReservationAsync(InventoryReservation reservation)
        {
            _context.InventoryReservations.Update(reservation);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateInventoryItemAsync(InventoryItem inventoryItem)
        {
            _context.InventoryItems.Update(inventoryItem);
            await _context.SaveChangesAsync();
        }
        public async Task<InventoryItem?> GetInventoryItemAsync(Guid inventoryId, string batchNumber)
        {
            return await _context.InventoryItems
                .FirstOrDefaultAsync(item =>
                    item.InventoryId == inventoryId &&
                    item.BatchNumber == batchNumber);
        }
    }
}
