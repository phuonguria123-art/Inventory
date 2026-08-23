using Inventory.Application.Suppliers;
using Inventory.Application.Suppliers.Dto;
using Inventory.Application.Warehouses;
using Inventory.Application.Warehouses.DTOs;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult> GetAll() => Ok(await _supplierService.GetAllAsync());
        [HttpGet("id")]
        public async Task<IActionResult> getById(Guid id) => Ok(await _supplierService.GetByIdAsync(id));
        [HttpPost]
        public async Task<IActionResult> Create(CreateSupplierDto dto)
        {
            await _supplierService.AddAsync(dto);
            return Ok();
        }
        [HttpPut]
        public async Task<IActionResult> Update(UpdateSupplierDto dto)
        {
            await _supplierService.UpdateAsync(dto);
            return Ok();
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supplierService.DeleteAsync(id);
            return Ok();
        }

    }
}
