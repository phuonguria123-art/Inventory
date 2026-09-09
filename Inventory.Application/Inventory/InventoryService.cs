using AutoMapper;
using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Inventory;

public sealed class InventoryService(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository,
    IWarehouseRepository warehouseRepository,
    IMapper mapper
    ) : IInventoryService
{
    public async Task<InventoryDto> GetAsync(Guid warehouseId, Guid productId)
    {
        if (warehouseId == Guid.Empty || productId == Guid.Empty)
            throw new ValidationException("Kho và sản phẩm là bắt buộc.");

        var inventory = await inventoryRepository.GetReadOnlyAsync(warehouseId, productId);
        if (inventory is null)
            throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");

        return mapper.Map<InventoryDto>(inventory);
    }

    public async Task<PagedResult<InventoryDto>> GetListAsync(
        int pageSize,
        int pageNumber,
        Guid? warehouseId,
        Guid? productId,
        string? warehouseSearch,
        string? productSearch,
        string? sortBy,
        bool sortDescending)
    {
        ValidatePagination(pageSize, pageNumber);
        ValidateOptionalId(warehouseId, "Mã định danh kho không hợp lệ.");
        ValidateOptionalId(productId, "Mã định danh sản phẩm không hợp lệ.");

        var (inventories, totalCount) = await inventoryRepository.GetAllAsync(
            pageSize,
            pageNumber,
            warehouseId,
            productId,
            warehouseSearch,
            productSearch,
            sortBy,
            sortDescending);

        return new PagedResult<InventoryDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = mapper.Map<List<InventoryDto>>(inventories)
        };
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetListTransaction(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference)
    {
        ValidatePagination(pageSize, pageNumber);

        var (inventoryTransactions, totalCount) = await inventoryRepository.GetAllTransactionAsync(
            pageSize,
            pageNumber,
            warehouseId,
            productId,
            transactionType,
            createdByUserId,
            reference);

        return new PagedResult<InventoryTransactionDto>
        {
            Items = mapper.Map<List<InventoryTransactionDto>>(inventoryTransactions),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }
    public async Task TransferAsync(InventoryTransferRequestDto request, Guid userId)
    {
        if (request.ProductId == Guid.Empty ||
            request.WarehouseFromId == Guid.Empty ||
            request.WarehouseToId == Guid.Empty)
        {
            throw new ValidationException("Sản phẩm, kho nguồn và kho đích là bắt buộc.");
        }

        if (request.WarehouseToId == request.WarehouseFromId)
            throw new ValidationException("Kho nguồn và kho đích phải khác nhau.");
        if (request.Quantity <= 0)
            throw new ValidationException("Số lượng chuyển phải lớn hơn 0.");

        if (!await productRepository.ExistByIdAsync(request.ProductId))
            throw new NotFoundException("Sản phẩm không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(request.WarehouseFromId))
            throw new NotFoundException("Kho nguồn không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(request.WarehouseToId))
            throw new NotFoundException("Kho đích không tồn tại.");

        var reference = NormalizeOrCreateReference(request.Reference);
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            var inventoryFrom = await inventoryRepository.GetAsync(
                request.WarehouseFromId,
                request.ProductId);
            if (inventoryFrom is null)
                throw new NotFoundException("Kho nguồn chưa có tồn kho của sản phẩm này.");
            if (inventoryFrom.AvailableQuantity < request.Quantity)
                throw new ConflictException("Số lượng khả dụng tại kho nguồn không đủ để chuyển.");

            var inventoryTo = await inventoryRepository.GetAsync(
                request.WarehouseToId,
                request.ProductId);
            if (inventoryTo is null)
            {
                inventoryTo = CreateInventory(request.WarehouseToId, request.ProductId);
                await inventoryRepository.AddAsync(inventoryTo);
            }

            inventoryFrom.QuantityOnHand -= request.Quantity;
            inventoryFrom.LastUpdate = DateTime.UtcNow;

            inventoryTo.QuantityOnHand = checked(inventoryTo.QuantityOnHand + request.Quantity);
            inventoryTo.LastUpdate = DateTime.UtcNow;

            await inventoryRepository.AddTransactionAsync(CreateTransaction(
                request.WarehouseFromId,
                request.ProductId,
                userId,
                InventoryTransactionType.TransferOut,
                request.Quantity,
                reference,
                request.Notes));

            await inventoryRepository.AddTransactionAsync(CreateTransaction(
                request.WarehouseToId,
                request.ProductId,
                userId,
                InventoryTransactionType.TransferIn,
                request.Quantity,
                reference,
                request.Notes));
        });

    }
    //giữ hàng/hủy giữ hàng
    public async Task<InventoryDto> Reservation(InventoryReservationRequestDto request, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateMovement(request.WarehouseId, request.ProductId, request.Quantity);
        if (userId == Guid.Empty)
            throw new ValidationException("Người thực hiện là bắt buộc.");

        var reference = NormalizeRequiredReference(request.Reference);
        if (!request.IsCancel && request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
            throw new ValidationException("Thời hạn giữ hàng phải lớn hơn thời điểm hiện tại.");

        await EnsureProductAndWarehouseExistAsync(request.ProductId, request.WarehouseId);

        Inventories? updatedInventory = null;
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepository.GetAsync(request.WarehouseId, request.ProductId);
            if (inventory is null)
                throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");

            var activeReservation = await inventoryRepository.GetActiveReservationAsync(
                inventory.Id,
                reference);

            if (request.IsCancel)
            {
                if (activeReservation is null)
                    throw new NotFoundException("Không tìm thấy lượt giữ hàng đang hoạt động.");
                if (activeReservation.Quantity != request.Quantity)
                    throw new ConflictException("Số lượng hủy phải bằng số lượng đang được giữ.");
                if (inventory.ReservedQuantity < activeReservation.Quantity)
                    throw new ConflictException("Dữ liệu số lượng giữ hàng không nhất quán.");

                inventory.ReservedQuantity -= activeReservation.Quantity;
                activeReservation.Status = InventoryReservationStatus.Released;
            }
            else
            {
                if (activeReservation is not null)
                    throw new ConflictException("Yêu cầu này đã giữ hàng trước đó.");
                if (inventory.AvailableQuantity < request.Quantity)
                    throw new ConflictException("Số lượng khả dụng không đủ để giữ hàng.");

                inventory.ReservedQuantity = checked(inventory.ReservedQuantity + request.Quantity);
                await inventoryRepository.AddReservationAsync(new InventoryReservation
                {
                    Id = Guid.NewGuid(),
                    InventoryId = inventory.Id,
                    Reference = reference,
                    Quantity = request.Quantity,
                    Status = InventoryReservationStatus.Active,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                });
            }

            inventory.LastUpdate = DateTime.UtcNow;

            updatedInventory = inventory;
        });

        return Map(updatedInventory!);
    }

    //hết hạn giữ hàng
    public async Task ReservationExpiredAsync()
    {
        var listReservation = await inventoryRepository.GetAllReservationExpired();
        if (listReservation.Count <= 0)
        {
            throw new NotFoundException("Không có sản phẩm nào tới hết hạn giữ hàng");
        }
        foreach (var reservation in listReservation)
        {
            reservation.Status = InventoryReservationStatus.Expired;
            var inventory = await inventoryRepository.GetById(reservation.InventoryId);
            if(inventory is null)
            {
                throw new NotFoundException("Không tìm thấy thông tin tồn kho tương ứng với sản phẩm hết hạn giữ hàng");
            }    
            inventory.ReservedQuantity -= reservation.Quantity;
            await inventoryRepository.UpdateRevationAsync(reservation);
            await inventoryRepository.UpdateAsync(inventory);
        }
    }
    //nhập kho
    public async Task<InventoryDto> ReceiveAsync(
        InventoryMovementRequestDto request,
        Guid userId)
    {
        ValidateMovement(request.WarehouseId, request.ProductId, request.Quantity);
        await EnsureProductAndWarehouseExistAsync(request.ProductId, request.WarehouseId);
        // Chỉ kiểm tra idempotency khi client cung cấp mã tham chiếu nghiệp vụ.
        var reference = request.Reference?.Trim();
        if (!string.IsNullOrWhiteSpace(reference) &&
            await inventoryRepository.TransactionExistsAsync(
                request.WarehouseId,
                request.ProductId,
                request.Quantity,
                InventoryTransactionType.Receipt,
                reference))
        {
            throw new ConflictException("Nhiệm vụ này đã được thực hiện trước đó.");
        }

        Inventories? updatedInventory = null;
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepository.GetAsync(request.WarehouseId, request.ProductId);
            if (inventory is null)
            {
                inventory = CreateInventory(request.WarehouseId, request.ProductId);
                await inventoryRepository.AddAsync(inventory);
            }

            inventory.QuantityOnHand = checked(inventory.QuantityOnHand + request.Quantity);
            inventory.LastUpdate = DateTime.UtcNow;

            await inventoryRepository.AddTransactionAsync(CreateTransaction(
                request.WarehouseId,
                request.ProductId,
                userId,
                InventoryTransactionType.Receipt,
                request.Quantity,
                request.Reference,
                request.Notes));

            updatedInventory = inventory;
        });

        return Map(updatedInventory!);
    }
    //xuất kho
    public async Task<InventoryDto> IssueAsync(
        InventoryMovementRequestDto request,
        Guid userId)
    {
        ValidateMovement(request.WarehouseId, request.ProductId, request.Quantity);
        await EnsureProductAndWarehouseExistAsync(request.ProductId, request.WarehouseId);
        // Chỉ kiểm tra idempotency khi client cung cấp mã tham chiếu nghiệp vụ.
        var reference = request.Reference?.Trim();
        if (!string.IsNullOrWhiteSpace(reference) &&
            await inventoryRepository.TransactionExistsAsync(
                request.WarehouseId,
                request.ProductId,
                request.Quantity,
                InventoryTransactionType.Issue,
                reference))
        {
            throw new ConflictException("Nhiệm vụ này đã được thực hiện trước đó.");
        }
        Inventories? updatedInventory = null;
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepository.GetAsync(request.WarehouseId, request.ProductId);
            if (inventory is null)
                throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");

            InventoryReservation? activeReservation = null;
            if (!string.IsNullOrWhiteSpace(request.Reference))
            {
                activeReservation = await inventoryRepository.GetActiveReservationAsync(
                    inventory.Id,
                    request.Reference.Trim());
            }

            if (activeReservation is not null)
            {
                if (activeReservation.ExpiresAt.HasValue && activeReservation.ExpiresAt.Value <= DateTime.UtcNow)
                    throw new ConflictException("Lượt giữ hàng đã hết hạn.");
                if (activeReservation.Quantity != request.Quantity)
                    throw new ConflictException("Số lượng xuất phải bằng số lượng đang được giữ.");
                if (inventory.QuantityOnHand < request.Quantity ||
                    inventory.ReservedQuantity < request.Quantity)
                {
                    throw new ConflictException("Dữ liệu số lượng giữ hàng không nhất quán.");
                }

                inventory.ReservedQuantity -= request.Quantity;
                activeReservation.Status = InventoryReservationStatus.Fulfilled;
            }
            else if (inventory.AvailableQuantity < request.Quantity)
            {
                throw new ConflictException("Số lượng khả dụng không đủ để xuất kho.");
            }

            inventory.QuantityOnHand -= request.Quantity;
            inventory.LastUpdate = DateTime.UtcNow;

            await inventoryRepository.AddTransactionAsync(CreateTransaction(
                request.WarehouseId,
                request.ProductId,
                userId,
                InventoryTransactionType.Issue,
                request.Quantity,
                request.Reference,
                request.Notes));

            updatedInventory = inventory;
        });

        return Map(updatedInventory!);
    }
    //kiểm kê
    public async Task<InventoryDto> AdjustAsync(
        InventoryAdjustmentRequestDto request,
        Guid userId)
    {
        if (request.WarehouseId == Guid.Empty || request.ProductId == Guid.Empty || request.Notes is null)
            throw new ValidationException("Kho, sản phẩm và lý do là bắt buộc.");
        if (request.ActualQuantity < 0)
            throw new ValidationException("Số lượng kiểm kê không được âm.");

        await EnsureProductAndWarehouseExistAsync(request.ProductId, request.WarehouseId);

        Inventories? updatedInventory = null;
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepository.GetAsync(request.WarehouseId, request.ProductId);
            if (inventory is null)
            {
                inventory = CreateInventory(request.WarehouseId, request.ProductId);
                await inventoryRepository.AddAsync(inventory);
            }

            if (request.ActualQuantity < inventory.ReservedQuantity)
                throw new ConflictException("Số lượng kiểm kê không thể nhỏ hơn số lượng đang được giữ chỗ.");

            var difference = request.ActualQuantity - inventory.QuantityOnHand;
            inventory.QuantityOnHand = request.ActualQuantity;
            inventory.LastUpdate = DateTime.UtcNow;

            if (difference != 0)
            {
                await inventoryRepository.AddTransactionAsync(CreateTransaction(
                    request.WarehouseId,
                    request.ProductId,
                    userId,
                    difference > 0
                        ? InventoryTransactionType.AdjustmentIncrease
                        : InventoryTransactionType.AdjustmentDecrease,
                    Math.Abs(difference),
                    request.Reference,
                    request.Notes));
            }

            updatedInventory = inventory;
        });

        return Map(updatedInventory!);
    }

    private async Task EnsureProductAndWarehouseExistAsync(Guid productId, Guid warehouseId)
    {
        if (!await productRepository.ExistByIdAsync(productId))
            throw new NotFoundException("Sản phẩm không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(warehouseId))
            throw new NotFoundException("Kho hàng không tồn tại.");
    }

    private static void ValidateMovement(Guid warehouseId, Guid productId, int quantity)
    {
        if (warehouseId == Guid.Empty || productId == Guid.Empty)
            throw new ValidationException("Kho và sản phẩm là bắt buộc.");
        if (quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");
    }

    private static void ValidatePagination(int pageSize, int pageNumber)
    {
        if (pageNumber < 1)
            throw new ValidationException("Số trang phải lớn hơn 0.");
        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("Kích thước trang phải từ 1 đến 100.");
    }

    private static void ValidateOptionalId(Guid? id, string message)
    {
        if (id.HasValue && id.Value == Guid.Empty)
            throw new ValidationException(message);
    }

    private static Inventories CreateInventory(Guid warehouseId, Guid productId) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        ProductId = productId,
        QuantityOnHand = 0,
        ReservedQuantity = 0,
        LastUpdate = DateTime.UtcNow
    };

    private static InventoryTransaction CreateTransaction(
        Guid warehouseId,
        Guid productId,
        Guid userId,
        InventoryTransactionType type,
        int quantity,
        string? reference,
        string? notes) => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductId = productId,
            CreatedByUserId = userId,
            TransactionType = type,
            Quantity = quantity,
            Reference = NormalizeOrCreateReference(reference),
            Notes = notes?.Trim(),
            TransactionDate = DateTime.UtcNow
        };

    private static string NormalizeOrCreateReference(string? reference)
    {
        return string.IsNullOrWhiteSpace(reference)
            ? $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31]
            : reference.Trim();
    }

    private static string NormalizeRequiredReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ValidationException("Mã tham chiếu giữ hàng là bắt buộc.");

        var normalizedReference = reference.Trim();
        if (normalizedReference.Length > 100)
            throw new ValidationException("Mã tham chiếu không được vượt quá 100 ký tự.");

        return normalizedReference;
    }

    private static InventoryDto Map(Inventories inventory) => new()
    {
        Id = inventory.Id,
        WarehouseId = inventory.WarehouseId,
        ProductId = inventory.ProductId,
        MinStock = inventory.MinStock,
        MaxStock = inventory.MaxStock,
        QuantityOnHand = inventory.QuantityOnHand,
        ReservedQuantity = inventory.ReservedQuantity,
        AvailableQuantity = inventory.AvailableQuantity,
        LastUpdate = inventory.LastUpdate
    };

    public async Task<List<InventoryDto>> GetLowOnStockAsync()
    {
        return mapper.Map<List<InventoryDto>>(await inventoryRepository.GetLowOnStockAsync());
    }

    public async Task<List<InventoryDto>> GetExcessGoodsAsync()
    {
        return mapper.Map<List<InventoryDto>>(await inventoryRepository.GetExcessGoodsAsync());
    }
}
