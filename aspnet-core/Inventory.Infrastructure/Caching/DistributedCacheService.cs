using Inventory.Application.Abstraction.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Caching
{
    public class DistributedCacheService(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger) : ICacheService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await distributedCache.GetAsync(key, cancellationToken);
                return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
               exception,
               "Không thể đọc Redis với key {CacheKey}. Sử dụng database.",
               key);
                return default;
            }

        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await distributedCache.RemoveAsync(key, cancellationToken);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning( exception, "Không thể xóa Redis với key {CacheKey}.",key);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
          
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await distributedCache.SetStringAsync(key, json,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = ttl
                        }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception,"Không thể ghi Redis với key {CacheKey}.", key);
            }
        }
    }
}
