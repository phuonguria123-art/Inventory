using AutoMapper;
using Inventory.Application.Warehouses.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Warehouses
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IMapper _mapper;
        private IWarehouseRepository _warehouseRepo;
        public WarehouseService(
             IMapper mapper,
             IWarehouseRepository warehouseRepo
        )
        {
            _warehouseRepo = warehouseRepo;
            _mapper = mapper;
        }
        public async Task<List<WarehouseDto>> GetAllAsync()
        {
            return _mapper.Map<List<WarehouseDto>>(await _warehouseRepo.GetAllAsync());
        }

        public async Task<WarehouseDto> GetByIdAsync(Guid id)
        {
            return _mapper.Map<WarehouseDto>(await _warehouseRepo.GetAsync(id));
        }
        public async Task AddAsync(CreateWarehouseDto product)
        {
            await _warehouseRepo.CreateAysnc(_mapper.Map<Warehouse>(product));
        }

        public async Task DeleteAsync(Guid id)
        {
            var warehouse = await _warehouseRepo.GetAsync(id);
            await _warehouseRepo.DeleteAsync(warehouse);
        }

        public async Task UpdateAsync(UpdateWarehouseDto product)
        {
            await _warehouseRepo.UpdateAsync(_mapper.Map<Warehouse>(product));
        }
    }
}
