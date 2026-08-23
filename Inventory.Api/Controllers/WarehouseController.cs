using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Services;
using Inventory.Application.Users.DTOs;
using Inventory.Application.Warehouses;
using Inventory.Application.Warehouses.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }
        [HttpGet]
        public async Task<ActionResult> GetAll() => Ok(await _warehouseService.GetAllAsync());
        [HttpGet("id")]
        public async Task<IActionResult> getById(Guid id) => Ok(await _warehouseService.GetByIdAsync(id));
        [HttpPost]
        public async Task<IActionResult> Create(CreateWarehouseDto dto)
        {
            await _warehouseService.AddAsync(dto);
            return Ok();
        }
        [HttpPut]
        public async Task<IActionResult> Update(UpdateWarehouseDto dto)
        {
            await _warehouseService.UpdateAsync(dto);
            return Ok();
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _warehouseService.DeleteAsync(id);
            return Ok();
        }
    }
}
