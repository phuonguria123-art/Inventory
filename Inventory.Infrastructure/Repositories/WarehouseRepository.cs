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
    public class WarehouseRepository(ApplicationDbContext _context) : IWarehouseRepository
    {
        public async Task CreateAysnc(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Warehouse warehouse)
        {
            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByIdAsync(Guid id)
        {
            return await _context.Warehouses.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
        {
            return await _context.Warehouses.AnyAsync(x => x.Code == code && (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task<Warehouse?> GetAsync(Guid id)
        {
            return await _context.Warehouses.FindAsync(id);
        }

        public async Task<List<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses.ToListAsync();
        }

        public async Task UpdateAsync(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            await _context.SaveChangesAsync();
        }
    }
}
