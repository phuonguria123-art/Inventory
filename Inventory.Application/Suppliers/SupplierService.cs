using AutoMapper;
using Inventory.Application.Suppliers.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Suppliers
{
    public class SupplierService : ISupplierService
    {
        private readonly IMapper _mapper;
        private ISupplierRepository _supplierRepo;
        public SupplierService(
             IMapper mapper,
             ISupplierRepository supplierRepo
        )
        {
            _supplierRepo = supplierRepo;
            _mapper = mapper;
        }
        public async Task AddAsync(CreateSupplierDto supplier)
        {
            await _supplierRepo.CreateAsync(_mapper.Map<Supplier>(supplier));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _supplierRepo.DeleteAsync(_mapper.Map<Supplier>(_supplierRepo.GetAsync(id)));
        }

        public async Task<List<SupplierDto>> GetAllAsync()
        {
            return _mapper.Map<List<SupplierDto>>(await _supplierRepo.GetAllAsycn());

        }

        public async Task<SupplierDto> GetByIdAsync(Guid id)
        {
            return _mapper.Map<SupplierDto>(await _supplierRepo.GetAsync(id));
        }

        public async Task UpdateAsync(UpdateSupplierDto product)
        {
           await _supplierRepo.UpdateAsync(_mapper.Map<Supplier>(product));
        }
    }
}
