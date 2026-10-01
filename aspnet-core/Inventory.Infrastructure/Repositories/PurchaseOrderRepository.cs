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
    public class PurchaseOrderRepository(ApplicationDbContext _context) : IPurchaseOrderRepository
    {

        public async Task<PurchaseOrder?> GetAsync(Guid id)
        {
            return await _context.PurchaseOrders.FindAsync(id);
        }
        public async Task<PurchaseOrder?> GetWithDetailsAsync(Guid id)
        {
            return await _context.PurchaseOrders
                .Include(order => order.OrderDetails)
                .FirstOrDefaultAsync(order => order.Id == id);
        }
        public async Task<List<PurchaseOrder>> GetListAsync()
        {
            return await _context.PurchaseOrders
              .Include(order => order.OrderDetails).ToListAsync();
        }
        public async Task CreateAsync(PurchaseOrder PurchaseOrder)
        {
            _context.PurchaseOrders.Add(PurchaseOrder);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PurchaseOrder PurchaseOrder)
        {
            _context.PurchaseOrders.Update(PurchaseOrder);
            await _context.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            if (!_context.Database.IsRelational() ||
                _context.Database.CurrentTransaction is not null)
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

        public async Task<(List<PurchaseOrder> items, int totalCount)> GetPageAsync(int pageSize, int pageNumber, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, DateTime? createDate)
        {
            var query = _context.PurchaseOrders.AsQueryable().AsNoTracking();
            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status);
            }
            if (supplierId.HasValue)
            {
                query = query.Where(x => x.SupplierId == supplierId);
            }
            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.ReceivingWarehouseId == warehouseId);
            }
            if (createDate.HasValue)
            {
                query = query.Where(x => x.CreateDate.Date == createDate.Value.Date);
            }
            var totalCount = await query.CountAsync();
            var result = await query.OrderBy(x => x.CreateDate).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (result, totalCount);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
