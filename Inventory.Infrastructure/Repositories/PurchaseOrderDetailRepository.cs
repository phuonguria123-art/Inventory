using Inventory.Domain.Entities;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class PurchaseOrderDetailRepository
    {
        private ApplicationDbContext _context;
        public PurchaseOrderDetailRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PurchanseOrderDetail?> GetAsync(Guid id)
        {
            return await _context.PurchanseOrdersDetails.FindAsync(id);
        }
        public async Task<List<PurchanseOrderDetail>> GetListAsync()
        {
            return await _context.PurchanseOrdersDetails.ToListAsync();
        }
        public async Task CreateAsync(PurchanseOrderDetail purchanseOrder)
        {
            _context.PurchanseOrdersDetails.Add(purchanseOrder);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PurchanseOrderDetail purchanseOrder)
        {
            _context.PurchanseOrdersDetails.Update(purchanseOrder);
            await _context.SaveChangesAsync();
        }
    }
}
