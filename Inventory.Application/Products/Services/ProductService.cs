using AutoMapper;
using Inventory.Domain.Interface;
using Inventory.Domain.Entity;
using Inventory.Application.Products.DTOs;
using Inventory.Application.Users.DTOs;

namespace Inventory.Application.Products.Services
{
    public class ProductService : IProductService
    {

        private readonly IProductRepository _productRepo;
        private readonly IMapper _mapper;
        public ProductService(IProductRepository productRepo, IMapper mapper)
        {
            _mapper = mapper;
            _productRepo = productRepo;
        }
        public async Task AddAsync(CreateProductDto product)
        {
            await _productRepo.AddAsync(_mapper.Map<Product>(product));
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return;
             await _productRepo.DeleteAsync(product);
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            return _mapper.Map<List<ProductDto>>(await _productRepo.GetAllAsync());
        }

        public async Task<ProductDto> GetByIdAsync(Guid id)
        {
            return _mapper.Map<ProductDto>(await _productRepo.GetByIdAsync(id));
        }

        public async Task UpdateAsync(UpdateProductDto product)
        {
            await _productRepo.UpdateAsync(_mapper.Map<Product>(product));
        }

    }
}
