using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Abstraction.Caching
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan ttl,
            CancellationToken cancellationToken);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
