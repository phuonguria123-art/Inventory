using Inventory.Application.Authorization;
using Inventory.Application.Suppliers;
using Inventory.Application.Suppliers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/suppliers")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [Authorize(Policy = PermissionCodes.SupplierRead)]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _supplierService.GetAllAsync();
            return Ok(result);
        }

        [Authorize(Policy = PermissionCodes.SupplierRead)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _supplierService.GetByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Policy = PermissionCodes.SupplierCreate)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            var supplier = await _supplierService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }

        [Authorize(Policy = PermissionCodes.SupplierUpdate)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
        {
            dto.Id = id;
            await _supplierService.UpdateAsync(dto);
            return NoContent();
        }

        [Authorize(Policy = PermissionCodes.SupplierDelete)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supplierService.DeleteAsync(id);
            return NoContent();
        }
    }
}
