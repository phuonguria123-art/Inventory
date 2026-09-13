using Inventory.Application.Authorization;
using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Application.Inventory.Dto.Transfer;
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
    public async Task<ActionResult<Inventory.Application.Common.Models.PagedResult<InventoryDto>>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? warehouseId = null,
        Guid? productId = null,
        string? warehouseSearch = null,
        string? productSearch = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        return Ok(await inventoryService.GetPagedAsync(
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
    public async Task<ActionResult<InventoryDto>> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId)
    {
        return Ok(await inventoryService.GetByWarehouseAndProductAsync(warehouseId, productId));
    }
    //hàng sắp hết
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryDto>>> GetLowStockAsync()
    {
        return Ok(await inventoryService.GetLowStockAsync());
    }
    //hàng vượt ngưỡng
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("excess-stock")]
    public async Task<ActionResult<List<InventoryDto>>> GetExcessStockAsync()
    {
        return Ok(await inventoryService.GetExcessStockAsync());
    }
    [Authorize(Policy = PermissionCodes.InventoryReceive)]
    [HttpPost("receive")]
    public async Task<ActionResult<List<InventoryDto>>> ReceiveAsync(ReceiveInventoryRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.ReceiveAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryIssue)]
    [HttpPost("issue")]
    public async Task<ActionResult<List<InventoryDto>>> IssueAsync(IssueInventoryRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.IssueAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryIssue)]
    [HttpPost("reservations")]
    public async Task<ActionResult<List<InventoryDto>>> ReserveAsync(ReserveInventoryRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.ReserveAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost("adjust")]
    public async Task<ActionResult<List<InventoryDto>>> AdjustAsync(AdjustInventoryRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await inventoryService.AdjustAsync(request, userId));
    }

    [Authorize(Policy = PermissionCodes.InventoryTransfer)]
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferAsync(TransferInventoryRequestDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await inventoryService.TransferAsync(request, userId);
        return NoContent();
    }
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactionsAsync(int pageNumber = 1, int pageSize = 20, Guid? warehouseId = null, Guid? productId = null, InventoryTransactionType? transactionType = null, Guid? createdByUserId = null, string? reference = null)
    {
        return Ok(await inventoryService.GetTransactionsAsync(pageSize, pageNumber, warehouseId, productId, transactionType, createdByUserId, reference));
    }
    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
