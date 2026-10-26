using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Reservation
{
    public interface IReservationExpirationService
    {
        Task<int> ExpireReservationsAsync(
        CancellationToken cancellationToken);
    }
}
