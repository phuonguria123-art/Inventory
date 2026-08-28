using AutoMapper;
using Inventory.Application.Warehouses.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;


namespace Inventory.Application.Warehouses
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IMapper _mapper;
        private readonly IWarehouseRepository _warehouseRepo;

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
            var warehouse = await _warehouseRepo.GetAsync(id);
            if (warehouse == null)
            {
                throw new NotFoundException("Kho hàng không tồn tại");
            }

            return _mapper.Map<WarehouseDto>(warehouse);
        }

        public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto product)
        {
            await ValidateWarehouse(product.Name, product.Code, product.Address, product.Phone);
            var warehouse = _mapper.Map<Warehouse>(product);
            await _warehouseRepo.CreateAsync(warehouse);
            return _mapper.Map<WarehouseDto>(warehouse);
        }

        public async Task DeleteAsync(Guid id)
        {
            var warehouse = await _warehouseRepo.GetAsync(id);
            if (warehouse == null)
            {
                throw new NotFoundException("Kho hàng không tồn tại");
            }

            await _warehouseRepo.DeleteAsync(warehouse);
        }

        public async Task UpdateAsync(UpdateWarehouseDto product)
        {
            var warehouse = await _warehouseRepo.GetAsync(product.Id);
            if (warehouse == null)
            {
                throw new NotFoundException("Kho hàng không tồn tại");
            }

            await ValidateWarehouse(product.Name, product.Code, product.Address, product.Phone, product.Id);

            _mapper.Map(product, warehouse);
            await _warehouseRepo.UpdateAsync(warehouse);
        }

        private async Task ValidateWarehouse(string name, string code, string address, string phone, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException("Tên kho không được bỏ trống");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ValidationException("Địa chỉ kho không được bỏ trống");
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ValidationException("Số điện thoại không được bỏ trống");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ValidationException("Mã kho không được để trống");
            }

            if (await _warehouseRepo.ExistsByCodeAsync(code, excludeId))
            {
                throw new ValidationException("Mã kho đã tồn tại");
            }
        }
    }
}
