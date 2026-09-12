using AutoMapper;
using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
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
        if (request is null)
            throw new ValidationException("Thông tin phiếu chuyển kho là bắt buộc.");

        ValidateTransferRequest(request, userId);

        if (!await warehouseRepository.ExistsByIdAsync(request.WarehouseFromId))
            throw new NotFoundException("Kho nguồn không tồn tại.");
        if (!await warehouseRepository.ExistsByIdAsync(request.WarehouseToId))
            throw new NotFoundException("Kho đích không tồn tại.");
        var hasClientReference = !string.IsNullOrWhiteSpace(request.Reference);

        var reference = NormalizeOrCreateReference(request.Reference);

        await inventoryRepository.ExecuteInTransactionAsync(async () =>
        {
            if (hasClientReference &&
                     await inventoryRepository.TransactionReferenceExistsAsync(
                         request.WarehouseFromId,
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
                var inventoryFrom = await inventoryRepository.GetAsync(
                    request.WarehouseFromId,
                    product.ProductId);
                if (inventoryFrom is null)
                    throw new NotFoundException("Kho nguồn chưa có tồn kho của sản phẩm này.");
                var totalProductRequest = product.Lots.Sum(l => l.Quantity);
                if (inventoryFrom.AvailableQuantity < totalProductRequest)
                    throw new ConflictException("Số lượng khả dụng tại kho nguồn không đủ để chuyển.");

                var inventoryTo = await inventoryRepository.GetAsync(
                   request.WarehouseToId,
                   product.ProductId);
                if (inventoryTo is null)
                {
                    inventoryTo = CreateInventory(request.WarehouseToId, product.ProductId);
                    await inventoryRepository.AddAsync(inventoryTo);
                }


                foreach (var lotRequest in product.Lots)
                {
                    var batchNumber = lotRequest.BatchNumber.Trim();
                    var lotFrom = await inventoryRepository.GetInventoryItemAsync(inventoryFrom.Id, batchNumber);
                    if (lotFrom is null)
                    {
                        throw new NotFoundException("Kho nguồn không tồn tại lô hàng này");
                    }
                    if (lotFrom.Quantity < lotRequest.Quantity)
                    {
                        throw new ConflictException("Số lượng khả dụng trong lô tại kho nguồn không đủ để chuyển.");
                    }
                    lotFrom.Quantity -= lotRequest.Quantity;

                    var lotTo = await inventoryRepository.GetInventoryItemAsync(inventoryTo.Id, batchNumber);
                    if (lotTo is null)
                    {
                        lotTo = CreateTransferredInventoryItem(
    lotFrom,
    inventoryTo.Id,
    lotRequest.Quantity,
    lotRequest.Location);
                        await inventoryRepository.AddInventoryItem(lotTo);
                    }
                    else
                    {
                        if (lotTo.UnitCost != lotFrom.UnitCost ||
    lotTo.ManufactureDate != lotFrom.ManufactureDate ||
    lotTo.ExpiryDate != lotFrom.ExpiryDate)
                        {
                            throw new ConflictException(
                                $"Lô '{lotFrom.BatchNumber}' tại kho đích có thông tin không khớp với kho nguồn.");
                        }
                        lotTo.Quantity = checked(
                            lotTo.Quantity + lotRequest.Quantity);
                    }
                    await inventoryRepository.AddTransactionAsync(CreateTransaction(
                  request.WarehouseFromId,
                  product.ProductId,
                  userId,
                  InventoryTransactionType.TransferOut,
                  lotRequest.Quantity,
                  reference,
                  request.Notes,
                  lotFrom.Id));

                    await inventoryRepository.AddTransactionAsync(CreateTransaction(
                        request.WarehouseToId,
                        product.ProductId,
                        userId,
                        InventoryTransactionType.TransferIn,
                        lotRequest.Quantity,
                        reference,
                        request.Notes,
                        lotTo.Id));

                }
                inventoryFrom.QuantityOnHand -= totalProductRequest;
                inventoryFrom.LastUpdate = DateTime.UtcNow;

                inventoryTo.QuantityOnHand = checked(inventoryTo.QuantityOnHand + totalProductRequest);
                inventoryTo.LastUpdate = DateTime.UtcNow;

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
        InventoryTransferRequestDto request,
        Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Người thực hiện phiếu chuyển kho không hợp lệ.");
        if (request.WarehouseFromId == Guid.Empty || request.WarehouseToId == Guid.Empty)
            throw new ValidationException("Kho nguồn và kho đích là bắt buộc.");
        if (request.WarehouseFromId == request.WarehouseToId)
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
        var hasClientReference = !string.IsNullOrWhiteSpace(request.Reference);
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
            if (inventory is null)
            {
                throw new NotFoundException("Không tìm thấy thông tin tồn kho tương ứng với sản phẩm hết hạn giữ hàng");
            }
            inventory.ReservedQuantity -= reservation.Quantity;
            await inventoryRepository.UpdateRevationAsync(reservation);
            await inventoryRepository.UpdateAsync(inventory);
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
        var updatedInventories = new List<Inventories>();

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
                var inventory = await ExecuteProductAsync(
                    request,
                    product,
                    userId,
                    reference);

                updatedInventories.Add(inventory);
            }
        });

        return updatedInventories.Select(Map).ToList();
    }

    private async Task<Inventories> ExecuteProductAsync(
        ReceiveInventoryRequestDto request,
        ReceiveProductDto productRequest,
        Guid userId,
        string reference)
    {
        var inventory = await inventoryRepository.GetAsync(
            request.WarehouseId,
            productRequest.ProductId);

        if (inventory is null)
        {
            inventory = CreateInventory(request.WarehouseId, productRequest.ProductId);
            await inventoryRepository.AddAsync(inventory);
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
                await inventoryRepository.AddInventoryItem(inventoryItem);
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
        inventory.LastUpdate = DateTime.UtcNow;

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

        var updatedInventories = new List<Inventories>();
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
                var inventory = await inventoryRepository.GetAsync(request.WarehouseId, product.ProductId);
                if (inventory is null)
                    throw new NotFoundException("Không tìm thấy tồn kho của sản phẩm tại kho này.");
                if (hasClientReference)
                {
                    activeReservation = await inventoryRepository.GetActiveReservationAsync(
                        inventory.Id,
                        reference);
                }

                var totalProductRequest = product.Lots.Sum(x => x.Quantity);

                if (activeReservation is not null)
                {
                    if (activeReservation.ExpiresAt.HasValue &&
                        activeReservation.ExpiresAt.Value <= DateTime.UtcNow)
                    {
                        throw new ConflictException("Lượt giữ hàng đã hết hạn.");
                    }

                    if (activeReservation.Quantity != totalProductRequest)
                        throw new ConflictException("Số lượng xuất phải bằng số lượng đang được giữ.");
                    if (inventory.QuantityOnHand < totalProductRequest ||
                        inventory.ReservedQuantity < totalProductRequest)
                    {
                        throw new ConflictException("Dữ liệu số lượng giữ hàng không nhất quán.");
                    }
                }
                else if (inventory.AvailableQuantity < totalProductRequest)
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
                    inventory.ReservedQuantity -= totalProductRequest;
                    activeReservation.Status = InventoryReservationStatus.Fulfilled;
                }

                inventory.QuantityOnHand -= totalProductRequest;
                inventory.LastUpdate = DateTime.UtcNow;

                updatedInventories.Add(inventory);
            }
        });

        return updatedInventories.Select(Map).ToList();
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
