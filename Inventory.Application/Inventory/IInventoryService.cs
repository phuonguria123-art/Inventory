using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Application.Inventory;

public interface IInventoryService
{
    Task<InventoryDto> GetAsync(Guid warehouseId, Guid productId);
    Task<PagedResult<InventoryDto>> GetListAsync(
        int pageSize,
        int pageNumber,
        Guid? warehouseId,
        Guid? productId,
        string? warehouseSearch,
        string? productSearch,
        string? sortBy,
        bool sortDescending);
    Task<PagedResult<InventoryTransactionDto>> GetListTransaction(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference);
    Task<List<InventoryDto>> GetLowOnStockAsync();
    Task<List<InventoryDto>> GetExcessGoodsAsync();
    Task<List<InventoryDto>> ReceiveAsync(ReceiveInventoryRequestDto request, Guid userId);
    Task<List<InventoryDto>> IssueAsync(IssueInventoryRequestDto request, Guid userId);
    Task<InventoryDto> AdjustAsync(InventoryAdjustmentRequestDto request, Guid userId);
    Task TransferAsync(InventoryTransferRequestDto request, Guid userId);
    Task<InventoryDto> Reservation(InventoryReservationRequestDto request, Guid userId);
    Task ReservationExpiredAsync();
}
