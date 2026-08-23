using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Services;
using Inventory.Application.Users.DTOs;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult> GetAll() => Ok(await _productService.GetAllAsync());
        [HttpGet("id")]
        public async Task<IActionResult> getById(Guid id) => Ok(await _productService.GetByIdAsync(id));
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            await _productService.AddAsync(dto);
            return Ok();
        }
        [HttpPut]
        public async Task<IActionResult> Update(UpdateProductDto dto)
        {
            await _productService.UpdateAsync(dto);
            return Ok();
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return Ok();
        }
    }
}
