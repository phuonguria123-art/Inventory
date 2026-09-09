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
        public async Task AddAsync(Inventories inventory)
        {
            await _context.Inventories.AddAsync(inventory);
        }

        public async Task AddTransactionAsync(InventoryTransaction transaction)
        {
            await _context.InventoryTransactions.AddAsync(transaction);
        }

        public async Task AddReservationAsync(InventoryReservation reservation)
        {
            await _context.InventoryReservations.AddAsync(reservation);
        }

        public async Task<(List<Inventories> listInventory, int totalCount)> GetAllAsync(
            int pageSize,
            int pageNumber,
            Guid? warehouseId,
            Guid? productId,
            string? warehouseSearch,
            string? productSearch,
            string? sortBy,
            bool sortDescending)
        {
            var query = _context.Inventories
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
                    ? query.OrderByDescending(x => x.LastUpdate)
                    : query.OrderBy(x => x.LastUpdate)
            };

            var result = await orderedQuery
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (result, totalCount);
        }

        public async Task<Inventories?> GetAsync(Guid warehouseId, Guid productId)
        {
            return await _context.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId && x.ProductId == productId);
        }
        public async Task<Inventories?> GetById(Guid inventoryId)
        {
            return await _context.Inventories.FirstOrDefaultAsync(x => x.Id == inventoryId);
        }

        public async Task<Inventories?> GetReadOnlyAsync(Guid warehouseId, Guid productId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId && x.ProductId == productId);
        }

        public async Task<bool> TransactionExistsAsync(
            Guid warehouseId,
            Guid productId,
            int quantity,
            InventoryTransactionType transactionType,
            string reference)
        {
            return await _context.InventoryTransactions.AnyAsync(x =>
                x.WarehouseId == warehouseId &&
                x.ProductId == productId &&
                x.Quantity == quantity &&
                x.TransactionType == transactionType &&
                x.Reference == reference);
        }

        public async Task<InventoryReservation?> GetActiveReservationAsync(Guid inventoryId, string reference)
        {
            return await _context.InventoryReservations.FirstOrDefaultAsync(reservation =>
                reservation.InventoryId == inventoryId &&
                reservation.Reference == reference &&
                reservation.Status == InventoryReservationStatus.Active);
        }
        public async Task<List<InventoryReservation>> GetAllReservationExpired()
        {
            return await _context.InventoryReservations.Where( x => x.ExpiresAt >= DateTime.UtcNow ).ToListAsync();
        }
        public async Task<(List<InventoryTransaction> listInventoryTransaction, int totalCount)> GetAllTransactionAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference)
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
            var inventoryTransaction = await query
                  .OrderByDescending(x => x.TransactionDate)
                  .ThenByDescending(x => x.Id)
                  .Skip((pageNumber - 1) * pageSize)
                  .Take(pageSize)
                  .ToListAsync();
            return (inventoryTransaction, totalCount);
        }
        public async Task<List<Inventories>> GetLowOnStockAsync()
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Product)
                .Where(x => x.QuantityOnHand - x.ReservedQuantity <= x.MinStock)
                .OrderBy(x => x.QuantityOnHand - x.ReservedQuantity)
                .ThenBy(x => x.Product.Name)
                .ToListAsync();
        }
        public async Task<List<Inventories>> GetExcessGoodsAsync()
        {
            return await _context.Inventories
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

        public async Task UpdateAsync(Inventories inventory)
        {
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRevationAsync(InventoryReservation reservation)
        {
            _context.InventoryReservations.Update(reservation);
            await _context.SaveChangesAsync();
        }
    }
}
