using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Application.Inventory.Dto.Transfer;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Application.Inventory;

public interface IInventoryService
{
    Task<InventoryDto> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId);
    Task<PagedResult<InventoryDto>> GetPagedAsync(
        int pageSize,
        int pageNumber,
        Guid? warehouseId,
        Guid? productId,
        string? warehouseSearch,
        string? productSearch,
        string? sortBy,
        bool sortDescending);
    Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference);
    Task<List<InventoryDto>> GetLowStockAsync();
    Task<List<InventoryDto>> GetExcessStockAsync();
    Task<List<InventoryDto>> ReceiveAsync(ReceiveInventoryRequestDto request, Guid userId);
    Task<List<InventoryDto>> IssueAsync(IssueInventoryRequestDto request, Guid userId);
    Task<List<InventoryDto>> AdjustAsync(AdjustInventoryRequestDto request, Guid userId);
    Task TransferAsync(TransferInventoryRequestDto request, Guid userId);
    Task<List<InventoryDto>> ReserveAsync(ReserveInventoryRequestDto request, Guid userId);
    Task ExpireReservationsAsync();
}
