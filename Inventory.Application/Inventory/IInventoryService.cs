using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory
{
    public interface IInventoryService
    {
        Task<Inventories> GetAsync(Guid warehouseid, Guid ProductId);
    }
}
