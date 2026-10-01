using AutoMapper;
using Inventory.Application.Common.Models;
using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Application.PurchaseOrders.Dto.Receive;
using Inventory.Application.PurrchanseOrderDetail.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders
{
    public class PurchaseOrderService(
        IPurchaseOrderRepository _purchaseOrderRepo,
        IWarehouseRepository _warehouseRepository,
        ISupplierRepository _supplierRepository,
        IProductRepository _productRepository,
        IInventoryService _inventoryService
        ) : IPurchaseOrderService
    {
        public async Task<PagedResult<PurchaseOrderDto>> GetPageAsync(int pageSize, int pageNumber, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, DateTime? createDate)
        {
            if (pageNumber <= 0)
            {
                throw new ValidationException("Số trang phải lớn hơn 0");
            }
            if (pageSize <= 0 || pageSize > 100)
            {
                throw new ValidationException("Kích thước trang phải từ 1 đến 100");
            }
            var (purchaseOrder, totalCount) = await _purchaseOrderRepo.GetPageAsync(pageSize, pageNumber, status, supplierId, warehouseId, createDate);
            var result = new List<PurchaseOrderDto>();
            foreach (var item in purchaseOrder)
            {
                var resultResult = new PurchaseOrderDto()
                {
                    Id = item.Id,
                    Code = item.Code,
                    SupplierId = item.SupplierId,
                    CreateDate = item.CreateDate,
                    SendDate = item.SendDate,
                    ReceivedDate = item.ReceivedDate,
                    ExpectedReceiveDate = item.ExpectedReceiveDate,
                    ReceivedTo = item.ReceivedTo,
                    ReceivingWarehouseId = item.ReceivingWarehouseId,
                    Freight = item.Freight,
                    Status = item.Status,
                    TotalPrice = item.TotalPrice,
                    TotalWeight = item.TotalWeight,
                    Type = item.Type,

                };
                result.Add(resultResult);
            }
            return new PagedResult<PurchaseOrderDto>() { Items = result, PageNumber = pageNumber, PageSize = pageSize, TotalCount = totalCount };
        }

        //chi tiết kèm dòng sản phẩm
        public async Task<PurchaseOrderDto> GetDetailAsync(Guid purchaseOrderId)
        {
            var purchaseOrder = await _purchaseOrderRepo.GetWithDetailsAsync(purchaseOrderId);
            if (purchaseOrder == null)
            {
                throw new NotFoundException("Đơn mua hàng không tồn tại");
            }

            List<PurchaseOrderDetailDto> listItemDetail = new List<PurchaseOrderDetailDto>();

            foreach (var itemDetail in purchaseOrder.OrderDetails)
            {
                var itemDtail = new PurchaseOrderDetailDto()
                {
                    PurchaseOrderId = itemDetail.PurchaseOrderId,
                    ProductId = itemDetail.ProductId,
                    OrderedQuantity = itemDetail.OrderedQuantity,
                    UnitPrice = itemDetail.UnitPrice,
                    Id = itemDetail.Id,
                    ActualReceivedQuantity = itemDetail.ActualReceivedQuantity,
                    Type = itemDetail.Type,
                    ReceiveTime = itemDetail.ReceiveTime,
                    InventoryId = itemDetail.InventoryId,
                    Note = itemDetail.Note
                };
                listItemDetail.Add(itemDtail);
            }
            var result = new PurchaseOrderDto()
            {
                Id = purchaseOrderId,
                Code = purchaseOrder.Code,
                SupplierId = purchaseOrder.SupplierId,
                CreateDate = purchaseOrder.CreateDate,
                SendDate = purchaseOrder.SendDate,
                ReceivedDate = purchaseOrder.ReceivedDate,
                ExpectedReceiveDate = purchaseOrder.ExpectedReceiveDate,
                ReceivedTo = purchaseOrder.ReceivedTo,
                ReceivingWarehouseId = purchaseOrder.ReceivingWarehouseId,
                Freight = purchaseOrder.Freight,
                Status = purchaseOrder.Status,
                TotalPrice = purchaseOrder.TotalPrice,
                TotalWeight = purchaseOrder.TotalWeight,
                Type = purchaseOrder.Type,
                OrderDetailDto = listItemDetail

            };
            return result;
        }
        // tạo mới đơn đặt hàng
        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderDto purchaneOrder)
        {
            if (purchaneOrder == null)
                throw new ValidationException("Thông tin đơn mua là bắt buộc.");
            await ValidateCreatePurchaseOrderAsync(purchaneOrder.SupplierId, purchaneOrder.ExpectedReceiveDate, purchaneOrder.ReceivingWarehouseId, purchaneOrder.Products, purchaneOrder.Freight, purchaneOrder.TotalWeight);
            //tạo đơn hàng
            var purchase = MapToPurchase(purchaneOrder);
            await _purchaseOrderRepo.CreateAsync(purchase);
            return purchase;

        }
        // cập nhật trạng thái Sent, confirm,shipping
        public async Task UpdateStatusAsync(Guid id, PurchaseOrderStatus status)
        {
            var order = await _purchaseOrderRepo.GetAsync(id);
            if (order == null)
                throw new NotFoundException("Đơn mua không tồn tại");
            if (!Enum.IsDefined(status))
            {
                throw new ValidationException("Trạng thái yêu cầu không hợp lệ");
            }

            if (order.Status == PurchaseOrderStatus.Received || order.Status == PurchaseOrderStatus.Cancelled)
            {
                throw new ValidationException("Không thể thay đổi trạng thái thành đã nhận hàng hoặc hủy đơn mua");
            }
            if (status == PurchaseOrderStatus.Sent && order.Status != PurchaseOrderStatus.Draft)
            {
                throw new ValidationException("Chỉ gửi đơn mua khi trạng thái hiện tại là nháp");
            }
            if (status == PurchaseOrderStatus.Draft)
            {
                throw new ValidationException("Chỉ thay đổi trạng thái nháp khi tạo đơn mua");
            }
            if (order.Status == PurchaseOrderStatus.Cancelled)
            {
                throw new ValidationException("Không thể thay đổi trạng thái khi đơn mua hàng đã bị hủy");
            }
            if (status == PurchaseOrderStatus.Cancelled)
            {
                throw new ValidationException(
                    "Hãy sử dụng chức năng hủy đơn mua.");
            }
            if (order.Status != PurchaseOrderStatus.Confirmed && status == PurchaseOrderStatus.Shipping)
            {
                throw new ValidationException("Chỉ giao hàng khi đơn mua đã được xác nhận");
            }
            if (order.Status != PurchaseOrderStatus.Sent && status == PurchaseOrderStatus.Confirmed)
            {
                throw new ValidationException("Chỉ xác nhận khi đơn mua đã được gửi");
            }
            if (status == PurchaseOrderStatus.Received)
            {
                throw new ValidationException("Không thể thay đổi trạng thái thành đã nhận hàng");
            }
            if (status == PurchaseOrderStatus.PartiallyReceived)
                throw new ValidationException(
                    "Hệ thống chưa hỗ trợ nhận đơn mua từng phần.");
            if (status == PurchaseOrderStatus.Sent && order.Status == PurchaseOrderStatus.Draft)
            {
                order.SendDate = DateTime.UtcNow;
            }

            order.Status = status;
            await _purchaseOrderRepo.UpdateAsync(order);

        }
        //Hủy PO
        public async Task CancelAsync(Guid purchaseOrderId)
        {
            var order = await _purchaseOrderRepo.GetAsync(purchaseOrderId);
            if (order is null)
            {
                throw new NotFoundException("Đơn mua không tồn tại");
            }
            if (order.Status == PurchaseOrderStatus.Received)
            {
                throw new ConflictException("Không thể thực hiện hủy khi đơn hàng đã hoàn thành");
            }
            if (order.Status == PurchaseOrderStatus.Cancelled)
            {
                throw new ConflictException("Đơn mua đã bị hủy trước đó");
            }
            order.Status = PurchaseOrderStatus.Cancelled;
            await _purchaseOrderRepo.UpdateAsync(order);

        }
        //Nhận hàng => kiểm đếm => xác nhận nhận hàng => cập nhật số lượng sản phẩm tương ứng
        public async Task<PurchaseOrder> OrderReceived(
            ReceivePurchaseOrderRequestDto request,
            Guid userId)
        {
            if (request is null)
                throw new ValidationException("Thông tin nhận đơn mua là bắt buộc.");

            if (request.PurchaseOrderId == Guid.Empty)
                throw new ValidationException("Mã đơn mua không hợp lệ.");

            if (userId == Guid.Empty)
                throw new ValidationException("Người nhận hàng không hợp lệ.");

            if (request.OrderDetails is null || request.OrderDetails.Count == 0)
                throw new ValidationException("Phải có ít nhất một dòng nhận hàng.");

            var order = await _purchaseOrderRepo.GetWithDetailsAsync(request.PurchaseOrderId);
            if (order is null)
                throw new NotFoundException("Đơn mua không tồn tại.");

            if (order.Status is not PurchaseOrderStatus.Sent
                and not PurchaseOrderStatus.Confirmed
                and not PurchaseOrderStatus.Shipping)
            {
                throw new ConflictException("Đơn mua không ở trạng thái cho phép nhận hàng.");
            }

            if (order.OrderDetails.Count == 0)
                throw new ConflictException("Đơn mua không có dòng sản phẩm để nhận.");

            var duplicateRequestDetail = request.OrderDetails
                .GroupBy(detail => detail.PurchaseOrderDetailId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateRequestDetail is not null)
            {
                throw new ValidationException(
                    $"Chi tiết đơn mua {duplicateRequestDetail.Key} bị gửi trùng.");
            }

            // Luồng hiện tại chỉ hỗ trợ nhận toàn bộ đơn trong một lần.
            if (request.OrderDetails.Count != order.OrderDetails.Count)
            {
                throw new ConflictException(
                    "Phiếu nhận phải bao gồm đầy đủ các dòng của đơn mua.");
            }

            var duplicateProduct = order.OrderDetails
                .GroupBy(detail => detail.ProductId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateProduct is not null)
            {
                throw new ConflictException(
                    $"Sản phẩm {duplicateProduct.Key} xuất hiện nhiều lần trong đơn mua. " +
                    "Hãy gộp thành một dòng trước khi nhận hàng.");
            }

            var orderDetailsById = order.OrderDetails.ToDictionary(detail => detail.Id);
            var receivedQuantities = new Dictionary<Guid, int>();
            var receiveProducts = new List<ReceiveProductDto>();

            foreach (var requestDetail in request.OrderDetails)
            {
                if (requestDetail.PurchaseOrderDetailId == Guid.Empty)
                    throw new ValidationException("Mã chi tiết đơn mua không hợp lệ.");

                if (!orderDetailsById.TryGetValue(requestDetail.PurchaseOrderDetailId, out var orderDetail))
                {
                    throw new NotFoundException(
                        $"Chi tiết đơn mua {requestDetail.PurchaseOrderDetailId} không thuộc đơn mua này.");
                }

                if (requestDetail.Lots is null || requestDetail.Lots.Count == 0)
                {
                    throw new ValidationException(
                        $"Chi tiết đơn mua {requestDetail.PurchaseOrderDetailId} phải có ít nhất một lô.");
                }

                if (orderDetail.ActualReceivedQuantity != 0)
                {
                    throw new ConflictException(
                        $"Chi tiết đơn mua {requestDetail.PurchaseOrderDetailId} đã được nhận trước đó.");
                }

                var actualReceivedQuantity = requestDetail.Lots.Sum(lot => (long)lot.ReceivedQuantity);
                if (actualReceivedQuantity > int.MaxValue)
                    throw new ValidationException("Tổng số lượng nhận vượt quá giới hạn cho phép.");

                if (actualReceivedQuantity != orderDetail.OrderedQuantity)
                {
                    throw new ConflictException(
                        $"Số lượng nhận của sản phẩm {orderDetail.ProductId} phải bằng số lượng đặt " +
                        $"({orderDetail.OrderedQuantity}).");
                }

                receivedQuantities[orderDetail.Id] = (int)actualReceivedQuantity;
                receiveProducts.Add(new ReceiveProductDto
                {
                    ProductId = orderDetail.ProductId,
                    Lots = requestDetail.Lots.Select(lot => new ReceiveLotDto
                    {
                        BatchNumber = lot.BatchNumber,
                        Quantity = lot.ReceivedQuantity,
                        UnitCost = lot.UnitCost,
                        ManufactureDate = lot.ManufactureDate,
                        ExpiryDate = lot.ExpiryDate,
                        Location = lot.Location
                    }).ToList()
                });
            }

            var receiveRequest = new ReceiveInventoryRequestDto
            {
                WarehouseId = order.ReceivingWarehouseId,
                PurchaseOrderId = order.Id,
                Reference = order.Code,
                Notes = $"Nhận hàng từ đơn mua {order.Code}",
                Products = receiveProducts
            };

            await _purchaseOrderRepo.ExecuteInTransactionAsync(async () =>
            {
                await _inventoryService.ReceiveAsync(receiveRequest, userId);

                var receivedAt = DateTime.UtcNow;
                foreach (var orderDetail in order.OrderDetails)
                {
                    orderDetail.ActualReceivedQuantity = receivedQuantities[orderDetail.Id];
                    orderDetail.ReceiveTime = receivedAt;
                }

                order.ReceivedDate = receivedAt;
                order.ReceivedTo = userId.ToString();
                order.Status = PurchaseOrderStatus.Received;
                await _purchaseOrderRepo.UpdateAsync(order);
            });

            return order;
        }
        private async Task ValidateUpdateRequestAsync(UpdatePurchaseOrderDto order)
        {
            if (order.SupplierId == Guid.Empty)
                throw new ValidationException("Mã nhà cung cấp không hợp lệ.");
            if (order.ReceivingWarehouseId == Guid.Empty)
                throw new ValidationException("Mã kho nhận không hợp lệ.");

            if (order.ExpectedReceiveDate.Date < DateTime.Now.Date)
                throw new ValidationException("Ngày dự kiến nhận hàng không hợp lệ.");
            if (order.Freight is not null && order.Freight < 0)
            {
                throw new ValidationException("Phí vận chuyển không được âm");
            }
            if (order.TotalWeight is not null && order.TotalWeight < 0)
            {
                throw new ValidationException("Tổng trọng lượng phải lớn hơn 0");
            }
            if (order.OrderDetails == null || order.OrderDetails.Count == 0)
            {
                throw new ConflictException("Đơn hàng phải có ít nhất 1 sp ");
            }
            var duplicateDetailId = order.OrderDetails
    .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
    .GroupBy(x => x.Id!.Value)
    .FirstOrDefault(group => group.Count() > 1);

            if (duplicateDetailId is not null)
            {
                throw new ValidationException(
                    $"Chi tiết đơn mua {duplicateDetailId.Key} bị gửi trùng.");
            }
            if (order.OrderDetails.Any(x => x.UnitPrice < 0)) { throw new ValidationException("giá sản phẩm không được âm"); }
            if (order.OrderDetails.Any(x => x.OrderedQuantity <= 0)) { throw new ValidationException("số lượng sản phẩm phải lớn hơn không"); }
            if (order.OrderDetails.Any(x => x.ProductId == Guid.Empty)) { throw new ValidationException("Mã sản phẩm không hợp lệ"); }
            if (!await _warehouseRepository.ExistsByIdAsync(order.ReceivingWarehouseId))
                throw new NotFoundException("Warehouse không tồn tại");

            if (!await _supplierRepository.ExistsByIdAsync(order.SupplierId))
                throw new NotFoundException("Supplier không tồn tại");
            var supplier = await _supplierRepository.GetAsync(order.SupplierId);
            if (supplier is not null && !supplier.IsActive)
            {
                throw new ValidationException("Nhà cung cấp đã ngừng hoạt động.");
            }

            var ids = order.OrderDetails.Select(x => x.ProductId).ToList();
            var notExistIds = await _productRepository.NotExits(ids);
            if (notExistIds.Any())
            {
                throw new NotFoundException($"Sản phẩm {notExistIds.First()} không tồn tại.");
            }
            var duplicateProduct = order.OrderDetails
                .GroupBy(x => x.ProductId)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateProduct != null)
            {
                throw new ValidationException($"Sản phẩm {duplicateProduct.Key} xuất hiện nhiều lần trong đơn mua. Hãy gộp thành một dòng trước khi tạo đơn.");
            }
        }
        private async Task ValidateCreatePurchaseOrderAsync(Guid SupplierId, DateTime ExpectedReceiveDate, Guid ReceivingWarehouseId, List<PurchaseOrderDetailDto> Products, decimal? freight, decimal? totalWeight)
        {
            if (SupplierId == Guid.Empty)
                throw new ValidationException("Mã nhà cung cấp không hợp lệ.");
            if (ReceivingWarehouseId == Guid.Empty)
                throw new ValidationException("Mã kho nhận không hợp lệ.");

            if (ExpectedReceiveDate.Date < DateTime.Now.Date)
                throw new ValidationException("Ngày dự kiến nhận hàng không hợp lệ.");
            if (freight is not null && freight < 0)
            {
                throw new ValidationException("Phí vận chuyển không được âm");
            }
            if (totalWeight is not null && totalWeight < 0)
            {
                throw new ValidationException("Tổng trọng lượng phải lớn hơn 0");
            }
            if (Products == null || Products.Count == 0)
            {
                throw new ConflictException("Đơn hàng phải có ít nhất 1 sp ");
            }
            if (Products.Any(x => x.UnitPrice < 0)) { throw new ValidationException("giá sản phẩm không được âm"); }
            if (Products.Any(x => x.OrderedQuantity <= 0)) { throw new ValidationException("số lượng sản phẩm phải lớn hơn không"); }
            if (Products.Any(x => x.ProductId == Guid.Empty)) { throw new ValidationException("Mã sản phẩm không hợp lệ"); }
            if (!await _warehouseRepository.ExistsByIdAsync(ReceivingWarehouseId))
                throw new NotFoundException("Warehouse không tồn tại");

            if (!await _supplierRepository.ExistsByIdAsync(SupplierId))
                throw new NotFoundException("Supplier không tồn tại");
            var supplier = await _supplierRepository.GetAsync(SupplierId);
            if (supplier is not null && !supplier.IsActive)
            {
                throw new ValidationException("Nhà cung cấp đã ngừng hoạt động.");
            }

            var ids = Products.Select(x => x.ProductId).ToList();
            var notExistIds = await _productRepository.NotExits(ids);
            if (notExistIds.Any())
            {
                throw new NotFoundException($"Sản phẩm {notExistIds.First()} không tồn tại.");
            }
            var duplicateProduct = Products
                .GroupBy(x => x.ProductId)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateProduct != null)
            {
                throw new ValidationException($"Sản phẩm {duplicateProduct.Key} xuất hiện nhiều lần trong đơn mua. Hãy gộp thành một dòng trước khi tạo đơn.");
            }
        }
        protected PurchaseOrder MapToPurchase(CreatePurchaseOrderDto createPurchase)
        {
            var id = Guid.NewGuid();
            decimal? totalPrice = 0;

            var order = new PurchaseOrder()
            {
                Id = id,
                Code = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid()}",
                SupplierId = createPurchase.SupplierId,
                CreateDate = DateTime.UtcNow,
                ExpectedReceiveDate = createPurchase.ExpectedReceiveDate,
                ReceivingWarehouseId = createPurchase.ReceivingWarehouseId,
                Status = PurchaseOrderStatus.Draft,
                Type = createPurchase.Type,
                TotalWeight = createPurchase.TotalWeight,
                Freight = createPurchase.Freight
            };

            foreach (var x in createPurchase.Products ?? new List<PurchaseOrderDetailDto>())
            {
                order.OrderDetails.Add(new PurchaseOrderDetail()
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = id,
                    ProductId = x.ProductId,
                    OrderedQuantity = x.OrderedQuantity,
                    UnitPrice = x.UnitPrice,
                    Note = x.Note ?? string.Empty
                });
                totalPrice += (x.UnitPrice * x.OrderedQuantity);
            }
            order.TotalPrice = totalPrice + (createPurchase.Freight ?? 0);
            return order;
        }

        public async Task UpdateAsync(
    Guid id,
    UpdatePurchaseOrderDto request)
        {
            if (request is null)
                throw new ValidationException(
                    "Thông tin đơn mua là bắt buộc.");

            var order = await _purchaseOrderRepo.GetWithDetailsAsync(id);

            if (order is null)
                throw new NotFoundException("Đơn mua không tồn tại.");

            if (order.Status != PurchaseOrderStatus.Draft)
                throw new ConflictException(
                    "Chỉ được cập nhật đơn mua ở trạng thái nháp.");

            await ValidateUpdateRequestAsync(request);

            var existingDetailsById = order.OrderDetails
                .ToDictionary(detail => detail.Id);

            foreach (var requestDetail in request.OrderDetails
                .Where(detail => detail.Id.HasValue && detail.Id.Value != Guid.Empty))
            {
                if (!existingDetailsById.ContainsKey(requestDetail.Id!.Value))
                {
                    throw new ValidationException(
                        $"Chi tiết {requestDetail.Id} không thuộc đơn mua này.");
                }
            }

            var requestExistingIds = request.OrderDetails
                .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // Xóa các dòng không còn trong request
            var removedDetails = order.OrderDetails
                .Where(x => !requestExistingIds.Contains(x.Id))
                .ToList();

            foreach (var removedDetail in removedDetails)
                order.OrderDetails.Remove(removedDetail);

            foreach (var requestDetail in request.OrderDetails)
            {
                if (!requestDetail.Id.HasValue ||
                    requestDetail.Id.Value == Guid.Empty)
                {
                    // Dòng mới
                    order.OrderDetails.Add(new PurchaseOrderDetail
                    {
                        PurchaseOrderId = order.Id,
                        ProductId = requestDetail.ProductId,
                        OrderedQuantity = requestDetail.OrderedQuantity,
                        UnitPrice = requestDetail.UnitPrice,
                        Type = requestDetail.Type,
                        Note = requestDetail.Note?.Trim() ?? string.Empty
                    });

                    continue;
                }

                // Dòng đã tồn tại
                var existingDetail = existingDetailsById[requestDetail.Id.Value];

                existingDetail.ProductId = requestDetail.ProductId;
                existingDetail.OrderedQuantity = requestDetail.OrderedQuantity;
                existingDetail.UnitPrice = requestDetail.UnitPrice;
                existingDetail.Type = requestDetail.Type;
                existingDetail.Note =
                    requestDetail.Note?.Trim() ?? string.Empty;
            }

            order.SupplierId = request.SupplierId;
            order.ReceivingWarehouseId = request.ReceivingWarehouseId;
            order.ExpectedReceiveDate = request.ExpectedReceiveDate;
            order.Freight = request.Freight;
            order.TotalWeight = request.TotalWeight;
            order.Type = request.Type;

            order.TotalPrice =
                order.OrderDetails.Sum(
                    x => x.UnitPrice * x.OrderedQuantity)
                + (order.Freight ?? 0);

            await _purchaseOrderRepo.SaveChangesAsync();
        }
    }
}
