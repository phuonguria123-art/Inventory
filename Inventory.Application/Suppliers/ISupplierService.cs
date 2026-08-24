using Inventory.Application.Suppliers.Dto;
using Inventory.Application.Warehouses.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Suppliers
{
    public interface ISupplierService
    {
        Task CreateAsync(CreateSupplierDto product);
        Task DeleteAsync(Guid id);
        Task<List<SupplierDto>> GetAllAsync();
        Task<SupplierDto> GetByIdAsync(Guid id);
        Task UpdateAsync(UpdateSupplierDto product);
    }
}
