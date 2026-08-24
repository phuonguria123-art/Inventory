using Inventory.Application.Suppliers;
using Inventory.Application.Suppliers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _supplierService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _supplierService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            await _supplierService.CreateAsync(dto);
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
        {
            dto.Id = id;
            await _supplierService.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supplierService.DeleteAsync(id);
            return NoContent();
        }
    }
}
