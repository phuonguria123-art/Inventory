using Inventory.Domain.Entities;
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
        public async Task CreateAsync(Inventories inventories)
        {
            _context.Inventories.Add(inventories);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Inventories inventories)
        {
           _context.Inventories.Remove(inventories);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Inventories>> GetAllAsync()
        {
            return await _context.Inventories.ToListAsync();
        }

        public async Task<Inventories?> GetAsync(Guid warehouseid, Guid ProductId)
        {
            return await _context.Inventories.Where(x => x.WarehouseId == warehouseid && x.ProductId == ProductId).FirstOrDefaultAsync();
        }
        public async Task UpdateAsync(Inventories inventories)
        {
            await _context.SaveChangesAsync();
        }
    }
}
