using AutoMapper;
using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Adjust;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Application.Inventory.Dto.Transfer;
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
    public async Task<InventoryDto> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId)
    {
        if (warehouseId == Guid.Empty || productId == Guid.Empty)
            throw new ValidationException("Kho và sản phẩm là bắt buộc.");

        var inventory = await inventoryRepository.GetReadOnlyByWarehouseAndProductAsync(warehouseId, productId);
        if (inventory is null)
            throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");

        return mapper.Map<InventoryDto>(inventory);
    }

    public async Task<PagedResult<InventoryDto>> GetPagedAsync(
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

        var (inventoryBalances, totalCount) = await inventoryRepository.GetPagedAsync(
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
            Items = mapper.Map<List<InventoryDto>>(inventoryBalances)
        };
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(int pageSize, int pageNumber, Guid? warehouseId, Guid? productId, InventoryTransactionType? transactionType, Guid? createdByUserId, string? reference)
    {
        ValidatePagination(pageSize, pageNumber);

        var (inventoryTransactions, totalCount) = await inventoryRepository.GetTransactionsAsync(
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
    public async Task TransferAsync(TransferInventoryRequestDto request, Guid userId)
    {
        if (request is null)
            throw new ValidationException("Thông tin phiếu chuyển kho là bắt buộc.");

        ValidateTransferRequest(request, userId);

        if (!await warehouseRepository.ExistsByIdAsync(request.SourceWarehouseId))
            throw new NotFoundException("Kho nguồn không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(request.DestinationWarehouseId))
            throw new NotFoundException("Kho đích không tồn tại.");
        var hasClientReference = !string.IsNullOrWhiteSpace(request.Reference);

        var reference = NormalizeOrCreateReference(request.Reference);

        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            if (hasClientReference &&
                     await inventoryRepository.TransactionReferenceExistsAsync(
                         request.SourceWarehouseId,
                         InventoryTransactionType.TransferOut,
                         reference))
            {
                throw new ConflictException("Phiếu chuyển kho này đã được thực hiện trước đó.");
            }
            foreach (var product in request.Products)
            {
                if (product.ProductId == Guid.Empty) { throw new ValidationException("Mã sản phảm là bắt buộc"); }
                if (!await productRepository.ExistByIdAsync(product.ProductId))
                    throw new NotFoundException("Sản phẩm không tồn tại.");
                var sourceInventory = await inventoryRepository.GetByWarehouseAndProductAsync(
                    request.SourceWarehouseId,
                    product.ProductId);
                if (sourceInventory is null)
                    throw new NotFoundException("Kho nguồn chưa có tồn kho của sản phẩm này.");
                var requestedQuantity = product.Lots.Sum(lot => lot.Quantity);
                if (sourceInventory.AvailableQuantity < requestedQuantity)
                    throw new ConflictException("Số lượng khả dụng tại kho nguồn không đủ để chuyển.");

                var destinationInventory = await inventoryRepository.GetByWarehouseAndProductAsync(
                   request.DestinationWarehouseId,
                   product.ProductId);
                if (destinationInventory is null)
                {
                    destinationInventory = CreateInventoryBalance(request.DestinationWarehouseId, product.ProductId);
                    await inventoryRepository.AddInventoryAsync(destinationInventory);
                }


                foreach (var lotRequest in product.Lots)
                {
                    var batchNumber = lotRequest.BatchNumber.Trim();
                    var sourceLot = await inventoryRepository.GetInventoryItemAsync(sourceInventory.Id, batchNumber);
                    if (sourceLot is null)
                    {
                        throw new NotFoundException("Kho nguồn không tồn tại lô hàng này");
                    }
                    if (sourceLot.Quantity < lotRequest.Quantity)
                    {
                        throw new ConflictException("Số lượng khả dụng trong lô tại kho nguồn không đủ để chuyển.");
                    }
                    sourceLot.Quantity -= lotRequest.Quantity;

                    var destinationLot = await inventoryRepository.GetInventoryItemAsync(destinationInventory.Id, batchNumber);
                    if (destinationLot is null)
                    {
                        destinationLot = CreateTransferredInventoryItem(
    sourceLot,
    destinationInventory.Id,
    lotRequest.Quantity,
    lotRequest.DestinationLocation);
                        await inventoryRepository.AddInventoryItemAsync(destinationLot);
                    }
                    else
                    {
                        if (destinationLot.UnitCost != sourceLot.UnitCost ||
    destinationLot.ManufactureDate != sourceLot.ManufactureDate ||
    destinationLot.ExpiryDate != sourceLot.ExpiryDate)
                        {
                            throw new ConflictException(
                                $"Lô '{sourceLot.BatchNumber}' tại kho đích có thông tin không khớp với kho nguồn.");
                        }
                        destinationLot.Quantity = checked(
                            destinationLot.Quantity + lotRequest.Quantity);
                    }
                    await inventoryRepository.AddTransactionAsync(CreateTransaction(
                  request.SourceWarehouseId,
                  product.ProductId,
                  userId,
                  InventoryTransactionType.TransferOut,
                  lotRequest.Quantity,
                  reference,
                  request.Notes,
                  sourceLot.Id));

                    await inventoryRepository.AddTransactionAsync(CreateTransaction(
                        request.DestinationWarehouseId,
                        product.ProductId,
                        userId,
                        InventoryTransactionType.TransferIn,
                        lotRequest.Quantity,
                        reference,
                        request.Notes,
                        destinationLot.Id));

                }
                sourceInventory.QuantityOnHand -= requestedQuantity;
                sourceInventory.LastUpdatedAt = DateTime.UtcNow;

                destinationInventory.QuantityOnHand = checked(destinationInventory.QuantityOnHand + requestedQuantity);
                destinationInventory.LastUpdatedAt = DateTime.UtcNow;

            }
        });

    }
    private static InventoryItem CreateTransferredInventoryItem(
    InventoryItem sourceLot,
    Guid destinationInventoryId,
    int quantity,
    string? destinationLocation) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = destinationInventoryId,
        BatchNumber = sourceLot.BatchNumber,
        Quantity = quantity,
        UnitCost = sourceLot.UnitCost,
        ManufactureDate = sourceLot.ManufactureDate,
        ExpiryDate = sourceLot.ExpiryDate,
        DateReceived = DateTime.UtcNow,
        Location = destinationLocation?.Trim() ?? string.Empty
    };

    private static void ValidateTransferRequest(
        TransferInventoryRequestDto request,
        Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Người thực hiện phiếu chuyển kho không hợp lệ.");
        if (request.SourceWarehouseId == Guid.Empty || request.DestinationWarehouseId == Guid.Empty)
            throw new ValidationException("Kho nguồn và kho đích là bắt buộc.");
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            throw new ValidationException("Kho nguồn và kho đích phải khác nhau.");
        if (request.Products is null || request.Products.Count == 0)
            throw new ValidationException("Phiếu chuyển kho phải có ít nhất một sản phẩm.");
        if (request.Reference?.Trim().Length > 100)
            throw new ValidationException("Mã tham chiếu không được vượt quá 100 ký tự.");
        if (request.Notes?.Trim().Length > 500)
            throw new ValidationException("Ghi chú không được vượt quá 500 ký tự.");

        foreach (var product in request.Products)
        {
            if (product.ProductId == Guid.Empty)
                throw new ValidationException("Sản phẩm trong phiếu chuyển kho không hợp lệ.");
            if (product.Lots is null || product.Lots.Count == 0)
                throw new ValidationException("Mỗi sản phẩm phải có ít nhất một lô cần chuyển.");

            foreach (var lot in product.Lots)
            {
                if (string.IsNullOrWhiteSpace(lot.BatchNumber))
                    throw new ValidationException("Mã lô là bắt buộc.");
                if (lot.BatchNumber.Trim().Length > 100)
                    throw new ValidationException("Mã lô không được vượt quá 100 ký tự.");
                if (lot.Quantity <= 0)
                    throw new ValidationException("Số lượng chuyển của lô phải lớn hơn 0.");
            }

            var duplicateLot = product.Lots
                .GroupBy(lot => lot.BatchNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateLot is not null)
            {
                throw new ValidationException(
                    $"Lô '{duplicateLot.Key}' bị lặp trong cùng một sản phẩm.");
            }

            var totalQuantity = product.Lots.Sum(lot => (long)lot.Quantity);
            if (totalQuantity > int.MaxValue)
                throw new ValidationException("Tổng số lượng chuyển của sản phẩm vượt quá giới hạn cho phép.");
        }

        var duplicateProduct = request.Products
            .GroupBy(product => product.ProductId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProduct is not null)
            throw new ValidationException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu chuyển kho.");
    }
    //giữ hàng/hủy giữ hàng
    private static void ValidateReservationRequest(ReserveInventoryRequestDto request, Guid userId)
    {
        if (request.WarehouseId == Guid.Empty)
            throw new ValidationException("Mã kho là bắt buộc");
        if (request.Products is null || request.Products.Count == 0)
        {
            throw new ValidationException("Danh sách phải có ít nhất một sản phẩm");
        }
        foreach (var product in request.Products)
        {
            if (product.ProductId == Guid.Empty)
                throw new ValidationException("Mã sản phẩm là bắt buộc");
            if (product.Quantity <= 0)
                throw new ValidationException("Số lượng phải lớn hơn 0.");
            if (userId == Guid.Empty)
                throw new ValidationException("Người thực hiện là bắt buộc.");

        }
        if (!request.IsCancellation && request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
            throw new ValidationException("Thời hạn giữ hàng phải lớn hơn thời điểm hiện tại.");

    }
    public async Task<List<InventoryDto>> ReserveAsync(ReserveInventoryRequestDto request, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateReservationRequest(request, userId);
        var reference = NormalizeRequiredReference(request.Reference);

        var updatedInventories = new List<InventoryBalance>();
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            foreach (var product in request.Products)
            {
                await EnsureProductAndWarehouseExistAsync(product.ProductId, request.WarehouseId);
                var inventory = await inventoryRepository.GetByWarehouseAndProductAsync(request.WarehouseId, product.ProductId);
                if (inventory is null)
                    throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");

                var activeReservation = await inventoryRepository.GetActiveReservationAsync(
                    inventory.Id,
                    reference);
                if (request.IsCancellation)
                {
                    if (activeReservation is null)
                        throw new NotFoundException("Không tìm thấy lượt giữ hàng đang hoạt động.");
                    if (activeReservation.Quantity != product.Quantity)
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
                    if (inventory.AvailableQuantity < product.Quantity)
                        throw new ConflictException("Số lượng khả dụng không đủ để giữ hàng.");

                    inventory.ReservedQuantity = checked(inventory.ReservedQuantity + product.Quantity);
                    await inventoryRepository.AddReservationAsync(new InventoryReservation
                    {
                        Id = Guid.NewGuid(),
                        InventoryId = inventory.Id,
                        Reference = reference,
                        Quantity = product.Quantity,
                        Status = InventoryReservationStatus.Active,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = request.ExpiresAt
                    });
                }
                inventory.LastUpdatedAt = DateTime.UtcNow;
                updatedInventories.Add(inventory);
            }

        });

        return updatedInventories.Select(MapToDto).ToList();
    }

    //hết hạn giữ hàng
    public async Task ExpireReservationsAsync()
    {
        var expiredReservations = await inventoryRepository.GetExpiredActiveReservationsAsync();
        if (expiredReservations.Count <= 0)
        {
            throw new NotFoundException("Không có sản phẩm nào tới hết hạn giữ hàng");
        }
        foreach (var reservation in expiredReservations)
        {
            reservation.Status = InventoryReservationStatus.Expired;
            var inventory = await inventoryRepository.GetByIdAsync(reservation.InventoryId);
            if (inventory is null)
            {
                throw new NotFoundException("Không tìm thấy thông tin tồn kho tương ứng với sản phẩm hết hạn giữ hàng");
            }
            inventory.ReservedQuantity -= reservation.Quantity;
            await inventoryRepository.UpdateReservationAsync(reservation);
            await inventoryRepository.UpdateInventoryAsync(inventory);
        }
    }
    //nhập kho
    public async Task<List<InventoryDto>> ReceiveAsync(
        ReceiveInventoryRequestDto request,
        Guid userId)
    {
        ValidateReceiveRequest(request, userId);

        var hasClientReference = !string.IsNullOrWhiteSpace(request.Reference);
        var reference = NormalizeOrCreateReference(request.Reference);
        var updatedInventories = new List<InventoryBalance>();

        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            // Một mã tham chiếu đại diện cho toàn bộ phiếu nhập. Nếu mã này đã có
            // giao dịch nhập thì từ chối để tránh cộng tồn hai lần khi client gửi lại.
            if (hasClientReference &&
                await inventoryRepository.TransactionReferenceExistsAsync(
                    request.WarehouseId,
                    InventoryTransactionType.Receipt,
                    reference))
            {
                throw new ConflictException("Phiếu nhập này đã được thực hiện trước đó.");
            }

            foreach (var product in request.Products)
            {
                await EnsureProductAndWarehouseExistAsync(product.ProductId, request.WarehouseId);
                var inventory = await ReceiveProductAsync(
                    request,
                    product,
                    userId,
                    reference);

                updatedInventories.Add(inventory);
            }
        });

        return updatedInventories.Select(MapToDto).ToList();
    }

    private async Task<InventoryBalance> ReceiveProductAsync(
        ReceiveInventoryRequestDto request,
        ReceiveProductDto productRequest,
        Guid userId,
        string reference)
    {
        var inventory = await inventoryRepository.GetByWarehouseAndProductAsync(
            request.WarehouseId,
            productRequest.ProductId);

        if (inventory is null)
        {
            inventory = CreateInventoryBalance(request.WarehouseId, productRequest.ProductId);
            await inventoryRepository.AddInventoryAsync(inventory);
        }

        foreach (var lot in productRequest.Lots)
        {
            var batchNumber = lot.BatchNumber.Trim();
            var inventoryItem = await inventoryRepository.GetInventoryItemAsync(
                inventory.Id,
                batchNumber);

            if (inventoryItem is null)
            {
                inventoryItem = CreateInventoryItem(lot, inventory.Id, batchNumber);
                await inventoryRepository.AddInventoryItemAsync(inventoryItem);
            }
            else
            {
                EnsureLotMetadataMatches(inventoryItem, lot);
                inventoryItem.Quantity = checked(inventoryItem.Quantity + lot.Quantity);
            }

            await inventoryRepository.AddTransactionAsync(CreateTransaction(
                request.WarehouseId,
                productRequest.ProductId,
                userId,
                InventoryTransactionType.Receipt,
                lot.Quantity,
                reference,
                request.Notes,
                inventoryItem.Id));
        }

        var receivedQuantity = productRequest.Lots.Sum(lot => lot.Quantity);
        inventory.QuantityOnHand = checked(inventory.QuantityOnHand + receivedQuantity);
        inventory.LastUpdatedAt = DateTime.UtcNow;

        return inventory;
    }

    private static InventoryItem CreateInventoryItem(
        ReceiveLotDto receiveLotDto,
        Guid inventoryId,
        string batchNumber) => new()
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            Quantity = receiveLotDto.Quantity,
            UnitCost = receiveLotDto.UnitCost,
            BatchNumber = batchNumber,
            ManufactureDate = receiveLotDto.ManufactureDate,
            ExpiryDate = receiveLotDto.ExpiryDate,
            DateReceived = DateTime.UtcNow,
            Location = receiveLotDto.Location?.Trim() ?? string.Empty
        };

    private static void EnsureLotMetadataMatches(
        InventoryItem inventoryItem,
        ReceiveLotDto receiveLotDto)
    {
        if (inventoryItem.UnitCost != receiveLotDto.UnitCost ||
            inventoryItem.ManufactureDate != receiveLotDto.ManufactureDate ||
            inventoryItem.ExpiryDate != receiveLotDto.ExpiryDate)
        {
            throw new ConflictException(
                $"Lô '{inventoryItem.BatchNumber}' đã tồn tại nhưng thông tin giá vốn, ngày sản xuất hoặc hạn sử dụng không khớp.");
        }
    }

    private static void ValidateReceiveRequest(
        ReceiveInventoryRequestDto request,
        Guid userId)
    {
        if (request is null)
            throw new ValidationException("Thông tin phiếu nhập là bắt buộc.");
        if (request.WarehouseId == Guid.Empty)
            throw new ValidationException("Kho nhập là bắt buộc.");
        if (userId == Guid.Empty)
            throw new ValidationException("Người thực hiện phiếu nhập không hợp lệ.");
        if (request.PurchaseOrderId == Guid.Empty)
            throw new ValidationException("Mã đơn mua không hợp lệ.");
        if (request.Products is null || request.Products.Count == 0)
            throw new ValidationException("Phiếu nhập phải có ít nhất một sản phẩm.");
        if (request.Reference?.Trim().Length > 100)
            throw new ValidationException("Mã tham chiếu không được vượt quá 100 ký tự.");
        if (request.Notes?.Trim().Length > 500)
            throw new ValidationException("Ghi chú không được vượt quá 500 ký tự.");

        var duplicateProduct = request.Products
            .GroupBy(product => product.ProductId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProduct is not null)
            throw new ValidationException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu nhập.");

        foreach (var product in request.Products)
        {
            if (product.ProductId == Guid.Empty)
                throw new ValidationException("Sản phẩm trong phiếu nhập không hợp lệ.");
            if (product.Lots is null || product.Lots.Count == 0)
                throw new ValidationException("Mỗi sản phẩm phải có ít nhất một lô.");

            var duplicateBatch = product.Lots
                .Where(lot => !string.IsNullOrWhiteSpace(lot.BatchNumber))
                .GroupBy(lot => lot.BatchNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateBatch is not null)
                throw new ValidationException(
                    $"Mã lô '{duplicateBatch.Key}' bị lặp trong cùng một sản phẩm.");

            foreach (var lot in product.Lots)
            {
                if (string.IsNullOrWhiteSpace(lot.BatchNumber))
                    throw new ValidationException("Mã lô là bắt buộc.");
                if (lot.BatchNumber.Trim().Length > 100)
                    throw new ValidationException("Mã lô không được vượt quá 100 ký tự.");
                if (lot.Quantity <= 0)
                    throw new ValidationException("Số lượng của lô phải lớn hơn 0.");
                if (lot.UnitCost < 0)
                    throw new ValidationException("Giá vốn của lô không được âm.");
                if (lot.ManufactureDate.HasValue &&
                    lot.ExpiryDate.HasValue &&
                    lot.ExpiryDate.Value < lot.ManufactureDate.Value)
                {
                    throw new ValidationException(
                        "Hạn sử dụng của lô không được trước ngày sản xuất.");
                }
            }
        }
    }
    private async Task ValidateIssueRequest(IssueInventoryRequestDto request,
            Guid userId)
    {
        if (request.WarehouseId == Guid.Empty)
            throw new ValidationException("Kho xuất là bắt buộc.");
        if (userId == Guid.Empty)
            throw new ValidationException("Người thực hiện là bắt buộc.");
        if (request.Products is null || request.Products.Count == 0)
            throw new ValidationException("Phiếu xuất phải có ít nhất một sản phẩm.");
        if (request.Reference?.Trim().Length > 100)
            throw new ValidationException("Mã tham chiếu không được vượt quá 100 ký tự.");
        if (request.Notes?.Trim().Length > 500)
            throw new ValidationException("Ghi chú không được vượt quá 500 ký tự.");
        var duplicateProduct = request.Products
                 .GroupBy(product => product.ProductId)
                 .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProduct is not null)
            throw new ValidationException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu xuất.");

        foreach (var product in request.Products)
        {

            if (product.ProductId == Guid.Empty)
                throw new ValidationException("Sản phẩm trong phiếu xuất không hợp lệ.");
            if (product.Lots is null || product.Lots.Count == 0)
                throw new ValidationException("Mỗi sản phẩm phải có ít nhất một lô.");
            await EnsureProductAndWarehouseExistAsync(product.ProductId, request.WarehouseId);
            var duplicateLot = product.Lots
              .Where(lot => !string.IsNullOrWhiteSpace(lot.BatchNumber))
                 .GroupBy(lot => lot.BatchNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                 .FirstOrDefault(group => group.Count() > 1);
            if (duplicateLot is not null)
                throw new ValidationException("Mỗi lô chỉ được xuất hiện một lần trong 1 sản phẩm.");

            foreach (var lot in product.Lots)
            {
                if (string.IsNullOrWhiteSpace(lot.BatchNumber))
                    throw new ValidationException("Mã lô là bắt buộc.");
                if (lot.BatchNumber.Trim().Length > 100)
                    throw new ValidationException("Mã lô không được vượt quá 100 ký tự.");
                if (lot.Quantity <= 0)
                    throw new ValidationException("Số lượng của lô phải lớn hơn 0.");
            }
        }
    }
    private async Task ValidateTransactionReferenceExistsAsync(Guid warehouseId, InventoryTransactionType type, string? reference)
    {
        if (!string.IsNullOrWhiteSpace(reference))
        {
            var isExists = await inventoryRepository.TransactionReferenceExistsAsync(warehouseId, type, reference);
            if (isExists)
            {
                throw new ConflictException("Yêu cầu đã được thực hiện trước đó");
            }
        }

    }
    //xuất kho
    public async Task<List<InventoryDto>> IssueAsync(
        IssueInventoryRequestDto request,
        Guid userId)
    {
        if (request is null)
            throw new ValidationException("Thông tin phiếu xuất là bắt buộc.");

        await ValidateIssueRequest(request, userId);
        var hasClientReference = !string.IsNullOrWhiteSpace(request.Reference);
        var reference = NormalizeOrCreateReference(request.Reference);

        var updatedInventories = new List<InventoryBalance>();
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            if (hasClientReference)
            {
                await ValidateTransactionReferenceExistsAsync(
                    request.WarehouseId,
                    InventoryTransactionType.Issue,
                    reference);
            }

            foreach (var product in request.Products)
            {
                InventoryReservation? activeReservation = null;
                var inventory = await inventoryRepository.GetByWarehouseAndProductAsync(request.WarehouseId, product.ProductId);
                if (inventory is null)
                    throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");
                if (hasClientReference)
                {
                    activeReservation = await inventoryRepository.GetActiveReservationAsync(
                        inventory.Id,
                        reference);
                }

                var requestedQuantity = product.Lots.Sum(lot => lot.Quantity);

                if (activeReservation is not null)
                {
                    if (activeReservation.ExpiresAt.HasValue &&
                        activeReservation.ExpiresAt.Value <= DateTime.UtcNow)
                    {
                        throw new ConflictException("Lượt giữ hàng đã hết hạn.");
                    }

                    if (activeReservation.Quantity != requestedQuantity)
                        throw new ConflictException("Số lượng xuất phải bằng số lượng đang được giữ.");
                    if (inventory.QuantityOnHand < requestedQuantity ||
                        inventory.ReservedQuantity < requestedQuantity)
                    {
                        throw new ConflictException("Dữ liệu số lượng giữ hàng không nhất quán.");
                    }
                }
                else if (inventory.AvailableQuantity < requestedQuantity)
                {
                    throw new ConflictException("Số lượng khả dụng không đủ để xuất kho.");
                }

                foreach (var lot in product.Lots)
                {
                    var batchNumber = lot.BatchNumber.Trim();
                    var inventoryItem = await inventoryRepository.GetInventoryItemAsync(
                        inventory.Id,
                        batchNumber);
                    if (inventoryItem is null)
                        throw new NotFoundException($"Không tìm thấy lô hàng '{batchNumber}' tương ứng.");

                    if (inventoryItem.Quantity < lot.Quantity)
                    {
                        throw new ConflictException($"Số lượng khả dụng trong lô '{batchNumber}' không đủ để xuất kho.");
                    }
                    if (inventoryItem.ExpiryDate.HasValue && inventoryItem.ExpiryDate.Value <= DateTime.UtcNow)
                    {
                        throw new ConflictException($"Lô hàng '{batchNumber}' đã hết hạn sử dụng.");
                    }

                    inventoryItem.Quantity -= lot.Quantity;

                    await inventoryRepository.AddTransactionAsync(CreateTransaction(
           request.WarehouseId,
           product.ProductId,
           userId,
           InventoryTransactionType.Issue,
           lot.Quantity,
           reference,
           request.Notes,
                    inventoryItem.Id));
                }

                if (activeReservation is not null)
                {
                    inventory.ReservedQuantity -= requestedQuantity;
                    activeReservation.Status = InventoryReservationStatus.Fulfilled;
                }

                inventory.QuantityOnHand -= requestedQuantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;

                updatedInventories.Add(inventory);
            }
        });

        return updatedInventories.Select(MapToDto).ToList();
    }
    //kiểm kê
    public async Task<List<InventoryDto>> AdjustAsync(
        AdjustInventoryRequestDto request,
        Guid userId)
    {
        var reference = NormalizeOrCreateReference(request.Reference);
        var updatedInventories = new List<InventoryBalance>();
        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            foreach (var product in request.Products)
            {
                if (request.WarehouseId == Guid.Empty || product.ProductId == Guid.Empty || request.Notes is null)
                    throw new ValidationException("Kho, sản phẩm và lý do là bắt buộc.");
                await EnsureProductAndWarehouseExistAsync(product.ProductId, request.WarehouseId);
                var inventory = await inventoryRepository.GetByWarehouseAndProductAsync(request.WarehouseId, product.ProductId);
                if (inventory is null)
                {
                    throw new NotFoundException("Không tìm thấy tồn kho cần kiểm kê.");
                }
                var totalDifference = 0;
                foreach (var lotRequest in product.Lots)
                {
                    if (lotRequest.ActualQuantity < 0)
                        throw new ValidationException("Số lượng kiểm kê không được âm.");
                    var lot = await inventoryRepository.GetInventoryItemAsync(inventory.Id, lotRequest.BatchNumber);
                    if (lot is null)
                    { throw new NotFoundException("Lô hàng không tồn tại"); }
                    var quantityDifference = lotRequest.ActualQuantity - lot.Quantity;
                    lot.Quantity = lotRequest.ActualQuantity;

                    if (quantityDifference != 0)
                    {
                        await inventoryRepository.AddTransactionAsync(CreateTransaction(
                            request.WarehouseId,
                            product.ProductId,
                            userId,
                            quantityDifference > 0
                                ? InventoryTransactionType.AdjustmentIncrease
                                : InventoryTransactionType.AdjustmentDecrease,
                            Math.Abs(quantityDifference),
                            reference,
                            request.Notes,
                            lot.Id));
                    }
                    totalDifference += quantityDifference;
                }
                var newQuantityOnHand = inventory.QuantityOnHand + totalDifference;
                if(newQuantityOnHand < inventory.ReservedQuantity)
                {
                    throw new ConflictException("Số lượng kiểm kê không được nhỏ hơn số lượng đang giữ");
                }
                inventory.QuantityOnHand = newQuantityOnHand;
                inventory.LastUpdatedAt = DateTime.UtcNow;
                updatedInventories.Add(inventory);
            }
        });

        return updatedInventories.Select(MapToDto).ToList();
    }

    private async Task EnsureProductAndWarehouseExistAsync(Guid productId, Guid warehouseId)
    {
        if (!await productRepository.ExistByIdAsync(productId))
            throw new NotFoundException("Sản phẩm không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(warehouseId))
            throw new NotFoundException("Kho hàng không tồn tại.");
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

    private static InventoryBalance CreateInventoryBalance(Guid warehouseId, Guid productId) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        ProductId = productId,
        QuantityOnHand = 0,
        ReservedQuantity = 0,
        LastUpdatedAt = DateTime.UtcNow
    };

    private static InventoryTransaction CreateTransaction(
        Guid warehouseId,
        Guid productId,
        Guid userId,
        InventoryTransactionType type,
        int quantity,
        string? reference,
        string? notes,
        Guid? inventoryItemId = null) => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductId = productId,
            CreatedByUserId = userId,
            TransactionType = type,
            Quantity = quantity,
            InventoryItemId = inventoryItemId,
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

    private static InventoryDto MapToDto(InventoryBalance inventory) => new()
    {
        Id = inventory.Id,
        WarehouseId = inventory.WarehouseId,
        ProductId = inventory.ProductId,
        MinStock = inventory.MinStock,
        MaxStock = inventory.MaxStock,
        QuantityOnHand = inventory.QuantityOnHand,
        ReservedQuantity = inventory.ReservedQuantity,
        AvailableQuantity = inventory.AvailableQuantity,
        LastUpdatedAt = inventory.LastUpdatedAt
    };

    public async Task<List<InventoryDto>> GetLowStockAsync()
    {
        return mapper.Map<List<InventoryDto>>(await inventoryRepository.GetLowStockAsync());
    }

    public async Task<List<InventoryDto>> GetExcessStockAsync()
    {
        return mapper.Map<List<InventoryDto>>(await inventoryRepository.GetExcessStockAsync());
    }
}
