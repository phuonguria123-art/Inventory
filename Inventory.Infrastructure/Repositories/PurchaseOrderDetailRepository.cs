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

        public async Task<PurchaseOrderDetail?> GetAsync(Guid id)
        {
            return await _context.PurchaseOrdersDetails.FindAsync(id);
        }
        public async Task<List<PurchaseOrderDetail>> GetListAsync()
        {
            return await _context.PurchaseOrdersDetails.ToListAsync();
        }
        public async Task CreateAsync(PurchaseOrderDetail PurchaseOrder)
        {
            _context.PurchaseOrdersDetails.Add(PurchaseOrder);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PurchaseOrderDetail PurchaseOrder)
        {
            _context.PurchaseOrdersDetails.Update(PurchaseOrder);
            await _context.SaveChangesAsync();
        }
    }
}
