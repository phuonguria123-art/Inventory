using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

        }

        public async Task<List<Guid>> NotExits(List<Guid> ids)
        {
            var existIds = await _context.Products
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();
            return ids.Except(existIds).ToList();
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Products.AsNoTracking().OrderBy(product => product.Name);
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistByIdAsync(Guid Id)
        {
            return await _context.Products.AnyAsync(x => x.Id == Id);
        }
        public async Task<bool> ExistByCodeAsync(string productCode, Guid? excludeId = null)
        {
            return await _context.Products.AnyAsync(x => x.Code == productCode && (!excludeId.HasValue || x.Id != excludeId.Value));
        }
        
    }
}
