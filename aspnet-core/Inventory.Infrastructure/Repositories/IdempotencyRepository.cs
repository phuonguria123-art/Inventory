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
    public class IdempotencyRepository : IIdempotencyRepository
    {
        readonly ApplicationDbContext _context;
        public IdempotencyRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IdempotencyRecord?> GetAsync(string key, string operation)
        {
            return await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record =>
                    record.Key == key &&
                    record.Operation == operation);
        }

        public async Task CreateAsync(IdempotencyRecord idempotencyRecord)
        {
            _context.IdempotencyRecords.Add(idempotencyRecord);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                throw new Inventory.Domain.Exceptions.ConflictException(
                    "Yêu cầu này đã được xử lý trước đó.",
                    exception);
            }
        }
    }
}
