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
    public class SupplierRepository : ISupplierRepository
    {
        readonly ApplicationDbContext _context;
        public SupplierRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        // Vì FindAsync() có thể không tìm thấy → trả về null nên cho Supplier trả về có thể null
        public async Task<Supplier?> GetAsync(Guid id)
        {
            return await _context.Suppliers.FindAsync(id);
        }
        public async Task<List<Supplier>> GetAllAsycn()
        {
            return await _context.Suppliers.ToListAsync();
        }
        public async Task CreateAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Supplier supplier)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
        }
        public Task<bool> ExistsAsync(Guid supplierId)
        {
            return _context.Suppliers
        .AnyAsync(x => x.Id == supplierId);
        }
    }
}
