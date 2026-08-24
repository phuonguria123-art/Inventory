using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            var product = await _productService.UpdateAsync(id, dto);
            return Ok(product);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
