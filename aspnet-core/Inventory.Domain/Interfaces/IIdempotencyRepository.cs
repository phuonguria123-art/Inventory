using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyRecord?> GetAsync(string key, string operation);
        Task CreateAsync(IdempotencyRecord idempotencyRecord);
    }
}
