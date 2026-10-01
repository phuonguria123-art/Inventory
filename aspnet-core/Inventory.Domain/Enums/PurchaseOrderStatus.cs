using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Enums
{
    public enum PurchaseOrderStatus
    {
        Draft = 0,
        Sent = 1,
        Confirmed = 2,
        Shipping = 3,
        PartiallyReceived = 4,
        Received = 5,
        Cancelled = 6
    }
}
