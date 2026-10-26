using Inventory.Application.Abstraction.Caching;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Reservation
{
    public class ReservationExpirationService : IReservationExpirationService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ReservationExpirationOptions _options;

        public ReservationExpirationService(
            IInventoryRepository inventoryRepository,
            IOptions<ReservationExpirationOptions> options,
            ICacheService cacheService)
        {
            _inventoryRepository = inventoryRepository;
            _options = options.Value;
            _cacheService = cacheService;
        }
        public async Task<int> ExpireReservationsAsync(
         CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var expiredCount = 0;

            await _inventoryRepository.ExecuteInTransactionAsync(
        async () =>
        {
            var reservations =
                await _inventoryRepository.GetExpiredActiveReservationsAsync(
                    now,
                    _options.BatchSize,
                    cancellationToken);

            foreach (var reservation in reservations)
            {
                var inventory = reservation.Inventory;

                if (inventory.ReservedQuantity < reservation.Quantity)
                {
                    throw new ConflictException(
                        "Dữ liệu giữ hàng không nhất quán.");
                }

                inventory.ReservedQuantity -= reservation.Quantity;
                reservation.Status =
                    InventoryReservationStatus.Expired;
            }

            expiredCount = reservations.Count;
        },
        cancellationToken);
            if (expiredCount > 0)
            {
                await _cacheService.RemoveAsync(
                       CacheKeys.Inventory.LowStock,
                       cancellationToken);

                await _cacheService.RemoveAsync(
                    CacheKeys.Inventory.ExcessStock,
                    cancellationToken);
            }
            return expiredCount;
        }
    }
}
