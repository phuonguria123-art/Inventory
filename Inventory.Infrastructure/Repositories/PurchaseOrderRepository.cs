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
    public class PurchaseOrderRepository(ApplicationDbContext _context) : IPurchaseOrderRepository
    {

        public async Task<PurchanseOrder?> GetAsync(Guid id)
        {
            return await _context.PurchanseOrders.FindAsync(id);
        }
        public async Task<List<PurchanseOrder>> GetListAsync()
        {
            return await _context.PurchanseOrders.ToListAsync();
        }
        public async Task CreateAsync(PurchanseOrder purchanseOrder)
        {
            _context.PurchanseOrders.Add(purchanseOrder);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PurchanseOrder purchanseOrder)
        {
            _context.PurchanseOrders.Update(purchanseOrder);
           await _context.SaveChangesAsync();
        }
    }
}
