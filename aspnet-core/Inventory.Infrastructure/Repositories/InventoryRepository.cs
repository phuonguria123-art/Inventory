using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
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

        public async Task<List<InventoryItem>> GetListItemAsync(Guid warehouseId,
       Guid productId)
        {
            return await _context.InventoryItems.Where(x => x.Inventory.ProductId == productId && x.Inventory.WarehouseId == warehouseId).OrderBy(x => x.ExpiryDate).ToListAsync();
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

        public async Task<List<InventoryReservation>>
    GetExpiredActiveReservationsAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken)
        {
            return await _context.InventoryReservations
                .Include(x => x.Inventory)
                .Where(x =>
                    x.Status == InventoryReservationStatus.Active &&
                    x.ExpiresAt.HasValue &&
                    x.ExpiresAt <= now)
                .OrderBy(x => x.ExpiresAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
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
                .Take(20)
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
                .Take(20)
                .ToListAsync();
        }
        public async Task ExecuteInTransactionAsync(
      Func<Task> operation,
      CancellationToken cancellationToken = default)
        {
            if (!_context.Database.IsRelational() ||
                _context.Database.CurrentTransaction is not null)
            {
                await operation();
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
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
        public async Task<List<InventoryMovementMonthlyResult>> GetMovementSummaryAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var fromDate = startDate.Date;
            var toDateExclusive = endDate.Date.AddDays(1);

            var query = _context.InventoryTransactions
                .AsNoTracking()
                .Where(x => x.ProductId == productId &&
                            x.TransactionDate >= fromDate &&
                            x.TransactionDate < toDateExclusive)
                .Where(x =>

                    x.TransactionType == InventoryTransactionType.Issue ||
                    x.TransactionType == InventoryTransactionType.Receipt);

            return await query.GroupBy(x =>new {x.TransactionDate.Year, x.TransactionDate.Month}).Select(group => new InventoryMovementMonthlyResult
            {
                Month = group.Key.Month,
                Year = group.Key.Year,
                TotalStockIn = group.Where(x => x.TransactionType == InventoryTransactionType.Receipt).Sum(x =>x.Quantity),
                TotalStockOut = group.Where(x => x.TransactionType == InventoryTransactionType.Issue).Sum(x => x.Quantity)
            }).OrderBy(x => x.Year).ThenBy(x =>  x.Month)
                .ToListAsync(cancellationToken);
        }
    }
}
