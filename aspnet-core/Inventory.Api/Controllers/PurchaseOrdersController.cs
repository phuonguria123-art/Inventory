using Inventory.Application.Authorization;
using Inventory.Application.PurchaseOrders;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Application.PurchaseOrders.Dto.Receive;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }
        // ds có phân trang, lọc
        [Authorize(Policy = PermissionCodes.PurchaseOrderRead)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 1, PurchaseOrderStatus? status = null, Guid? supplierId = null, Guid? warehouseId = null, DateTime? createDate = null)
        {
            return Ok(await _purchaseOrderService.GetPageAsync(pageSize, pageIndex, status, supplierId, warehouseId, createDate));

        }
        //chi tiết kèm dòng sản phẩm
        [Authorize(Policy = PermissionCodes.PurchaseOrderRead)]
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetDetailAsync(Guid id)
        {
            return Ok(await _purchaseOrderService.GetDetailAsync(id));
        }
        //tạo PO nháp
        [Authorize(Policy = PermissionCodes.PurchaseOrderCreate)]
        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreatePurchaseOrderDto purchaseOrder)
        {
            await _purchaseOrderService.CreateAsync(purchaseOrder);
            return NoContent();
        }

        [Authorize(Policy = PermissionCodes.PurchaseOrderUpdate)]
        [HttpPut("{id:Guid}/status")]
        public async Task<IActionResult> UpdateStatusAsync(Guid id, PurchaseOrderStatus status)
        {
            await _purchaseOrderService.UpdateStatusAsync(id, status);
            return NoContent();
        }
        [Authorize(Policy = PermissionCodes.PurchaseOrderUpdate)]
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, UpdatePurchaseOrderDto purchaseOrder)
        {
            await _purchaseOrderService.UpdateAsync(id, purchaseOrder);
            return NoContent();
        }

        [Authorize(Policy = PermissionCodes.PurchaseOrderReceive)]
        [HttpPut("receive")]
        public async Task<IActionResult> ReceivedAsync(ReceivePurchaseOrderRequestDto request)
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(claimValue, out var userId))
                return Unauthorized();
            await _purchaseOrderService.OrderReceived(request, userId);
            return NoContent();
        }
        //Hủy PO 
        [Authorize(Policy = PermissionCodes.PurchaseOrderCancel)]
        [HttpPut("cancel")]
        public async Task<IActionResult> CancelAsync(Guid id)
        {
            await _purchaseOrderService.CancelAsync(id);
            return Ok();
        }

    }
}
