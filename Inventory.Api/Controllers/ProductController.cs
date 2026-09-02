using Inventory.Application.Authorization;
using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [Authorize(Policy = PermissionCodes.ProductRead)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var products = await _productService.GetAllAsync(pageNumber, pageSize);
            return Ok(products);
        }

        [Authorize(Policy = PermissionCodes.ProductRead)]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        [Authorize(Policy = PermissionCodes.ProductCreate)]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [Authorize(Policy = PermissionCodes.ProductUpdate)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id,[FromBody] UpdateProductDto dto)
        {
            dto.Id = id;
            await _productService.UpdateAsync( dto);
            return NoContent();
        }

        [Authorize(Policy = PermissionCodes.ProductDelete)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
