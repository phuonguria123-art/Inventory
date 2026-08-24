using AutoMapper;
using Inventory.Application.Suppliers.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Suppliers
{
    public class SupplierService : ISupplierService
    {
        private readonly IMapper _mapper;
        private readonly ISupplierRepository _supplierRepo;

        public SupplierService(
             IMapper mapper,
             ISupplierRepository supplierRepo
        )
        {
            _supplierRepo = supplierRepo;
            _mapper = mapper;
        }

        public async Task DeleteAsync(Guid id)
        {
            var supplier = await _supplierRepo.GetAsync(id);
            if (supplier == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp");
            }

            await _supplierRepo.DeleteAsync(supplier);
        }

        public async Task<List<SupplierDto>> GetAllAsync()
        {
            return _mapper.Map<List<SupplierDto>>(await _supplierRepo.GetAllAsycn());
        }

        public async Task<SupplierDto> GetByIdAsync(Guid id)
        {
            var supplier = await _supplierRepo.GetAsync(id);
            if (supplier == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp");
            }

            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task CreateAsync(CreateSupplierDto dto)
        {
            await ValidateSupplier(dto.Name, dto.Address, dto.Phone, dto.Code, null);

            var supplier = _mapper.Map<Supplier>(dto);
            supplier.Id = Guid.NewGuid();
            await _supplierRepo.CreateAsync(supplier);
        }

        public async Task UpdateAsync(UpdateSupplierDto dto)
        {
            var supplier = await _supplierRepo.GetAsync(dto.Id);
            if (supplier == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp để cập nhật");
            }

            await ValidateSupplier(dto.Name, dto.Address, dto.Phone, dto.Code, dto.Id);
            _mapper.Map(dto, supplier);

            await _supplierRepo.UpdateAsync(supplier);
        }

        private async Task ValidateSupplier(string name, string address, string phone, string? code, Guid? excludeId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException("Tên không được bỏ trống");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ValidationException("Địa chỉ không được bỏ trống");
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ValidationException("Số điện thoại không được bỏ trống");
            }

            if (!string.IsNullOrWhiteSpace(code) && await _supplierRepo.ExistsByCodeAsync(code, excludeId))
            {
                throw new ValidationException("Mã NCC đã tồn tại");
            }
        }
    }
}
