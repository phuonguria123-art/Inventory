using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Reservation
{
    public class ReservationExpirationOptions
    {
        public const string SectionName = "ReservationExpiration";

        public int CheckIntervalMinutes { get; set; } = 5;

        public int BatchSize { get; set; } = 100;
    }
}
