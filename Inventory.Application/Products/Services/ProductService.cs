using AutoMapper;
using Inventory.Application.Products.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Products.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepo, IMapper mapper, ISupplierRepository supplierRepo)
        {
            _productRepo = productRepo;
            _mapper = mapper;
            _supplierRepo = supplierRepo;
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
           await ValidateProduct(dto.Name, dto.Code, dto.SupplierId);
            var exists = await _productRepo.ExistByCodeAsync(dto.Code, null);

            if (exists)
                throw new ValidationException("Mã sản phẩm đã tồn tại");

            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();

            await _productRepo.AddAsync(product);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Không tìm thấy sản phẩm");

            await _productRepo.DeleteAsync(product);
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            var products = await _productRepo.GetAllAsync();
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<ProductDto> GetByIdAsync(Guid id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Không tìm thấy sản phẩm");

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> UpdateAsync( UpdateProductDto dto)
        {
            await ValidateProduct(dto.Name, dto.Code, dto.SupplierId);
            var existingProduct = await _productRepo.GetByIdAsync(dto.Id);
            if (existingProduct == null)
                throw new NotFoundException("Không tìm thấy sản phẩm để cập nhật");
            var duplicateProduct = await _productRepo.ExistByCodeAsync(dto.Code, dto.Id);
            if (duplicateProduct)
                throw new ValidationException("Mã sản phẩm đã tồn tại");
            existingProduct.Name = dto.Name;
            existingProduct.Code = dto.Code;
            existingProduct.ImgUrl = dto.ImgUrl;
            existingProduct.Origin = dto.Origin;
            existingProduct.Weight = dto.Weight;

            await _productRepo.UpdateAsync(existingProduct);
            return _mapper.Map<ProductDto>(existingProduct);
        }

        private async Task ValidateProduct(string name, string code, Guid? supplierId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Tên sản phẩm không được để trống");

            if (string.IsNullOrWhiteSpace(code))
                throw new ValidationException("Mã sản phẩm không được để trống");
            if (supplierId.HasValue)
            {
                var supplierExist = await _supplierRepo.ExistsByIdAsync(supplierId.Value);
                if (!supplierExist)
                    throw new ValidationException("Nhà cung cấp không hợp lệ");
            }
        }
    }
}
