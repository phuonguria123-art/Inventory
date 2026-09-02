
using Inventory.Application.Authorization;
using Inventory.Application.Warehouses;
using Inventory.Application.Warehouses.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/warehouses")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [Authorize(Policy = PermissionCodes.WarehouseRead)]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _warehouseService.GetAllAsync();
            return Ok(result);
        }
        [Authorize(Policy = PermissionCodes.WarehouseRead)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _warehouseService.GetByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Policy = PermissionCodes.WarehouseCreate)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        {
            var warehouse = await _warehouseService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
        }

        [Authorize(Policy =PermissionCodes.WarehouseUpdate)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseDto dto)
        {
            dto.Id = id;
            await _warehouseService.UpdateAsync(dto);
            return NoContent();
        }
        [Authorize(Policy = PermissionCodes.WarehouseDelete)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _warehouseService.DeleteAsync(id);
            return NoContent();
        }
    }
}
