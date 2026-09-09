using Inventory.Application.Authorization;
using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Dto;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.Api.Controllers;

[Route("api/inventory")]
[ApiController]
public sealed class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet]
    public async Task<ActionResult<Inventory.Application.Common.Models.PagedResult<InventoryDto>>> GetListAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? warehouseId = null,
        Guid? productId = null,
        string? warehouseSearch = null,
        string? productSearch = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        return Ok(await inventoryService.GetListAsync(
            pageSize,
            pageNumber,
            warehouseId,
            productId,
            warehouseSearch,
            productSearch,
            sortBy,
            sortDescending));
    }
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("warehouses/{warehouseId:guid}/products/{productId:guid}")]
    public async Task<ActionResult<InventoryDto>> Get(Guid warehouseId, Guid productId)
    {
        return Ok(await inventoryService.GetAsync(warehouseId, productId));
    }
    //hàng sắp hết
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryDto>>> GetListLowOnStock()
    {
        return Ok(await inventoryService.GetLowOnStockAsync());
    }
    //hàng vượt ngưỡng
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("excess-stock")]
    public async Task<ActionResult<List<InventoryDto>>> GetListExcessGoodsAsync()
    {
        return Ok(await inventoryService.GetExcessGoodsAsync());
    }
    [Authorize(Policy = PermissionCodes.InventoryReceive)]
    [HttpPost("receive")]
    public async Task<ActionResult<InventoryDto>> Receive(InventoryMovementRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.ReceiveAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryIssue)]
    [HttpPost("issue")]
    public async Task<ActionResult<InventoryDto>> Issue(InventoryMovementRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.IssueAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryIssue)]
    [HttpPost("reservations")]
    public async Task<ActionResult<InventoryDto>> Reserve(InventoryReservationRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.Reservation(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost("adjust")]
    public async Task<ActionResult<InventoryDto>> Adjust(InventoryAdjustmentRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.AdjustAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryTransfer)]
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(InventoryTransferRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await inventoryService.TransferAsync(request, userId);
        return NoContent();
    }
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("transactions")]
    public async Task<IActionResult> InventoryTransaction(int pageNumber = 1, int pageSize = 20, Guid? warehouseId = null, Guid? productId = null, InventoryTransactionType? transactionType = null, Guid? createdByUserId = null, string? reference = null)
    {
        return Ok( await inventoryService.GetListTransaction(pageSize, pageNumber, warehouseId, productId, transactionType, createdByUserId, reference));
    }
    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
