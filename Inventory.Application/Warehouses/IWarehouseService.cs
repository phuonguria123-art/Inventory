using Inventory.Application.Products.DTOs;
using Inventory.Application.Users.DTOs;
using Inventory.Application.Warehouses.DTOs;
using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Warehouses
{
    public interface IWarehouseService
    {
        Task AddAsync(CreateWarehouseDto product);
        Task DeleteAsync(Guid id);
        Task<List<WarehouseDto>> GetAllAsync();
        Task<WarehouseDto> GetByIdAsync(Guid id);
        Task UpdateAsync(UpdateWarehouseDto product);
    }
}
