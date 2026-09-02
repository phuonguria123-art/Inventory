using AutoMapper;
using Inventory.Application.PurchaseOrders.Dto;
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
    public class PurchanseOrderService(
        IPurchaseOrderRepository _purchaseOrderRepo,
        IWarehouseRepository _warehouseRepository,
        ISupplierRepository _supplierRepository,
        IProductRepository _productRepository
        )
    {
        private static int counter = 0;
        // tạo mới đơn đặt hàng
        public async Task<PurchanseOrder> Create(CreatePurchaseOrderDto purchanseOrder)
        {
            //validate supplier,warehouse,product 
            if (!await _warehouseRepository.ExistsByIdAsync(purchanseOrder.WarehouseId))
                throw new Exception("Warehouse không tồn tại");

            if (!await _supplierRepository.ExistsByIdAsync(purchanseOrder.SupplierId))
                throw new Exception("Supplier không tồn tại");
            if (purchanseOrder.Products == null || purchanseOrder.Products.Count == 0)
            {
                throw new Exception("Đơn hành phải có ít nhất 1 sp ");
            }
            var ids = purchanseOrder.Products.Select(x => x.ProductId).ToList();
            var notExistIds = await _productRepository.NotExits(ids);
            if (notExistIds.Any())
            {
                throw new Exception($"Product {notExistIds.First()} không tồn tại.");
            }
            //tạo đơn hàng
            var purchanse = MapToPurchanse(purchanseOrder);
            await _purchaseOrderRepo.CreateAsync(purchanse);
            return purchanse;

        }
        // cập nhật trạng thái Sent, confirm,shipping
        public async Task UpdateStatus(Guid id, PurchaseOrderStatus status)
        {
            var order = await _purchaseOrderRepo.GetAsync(id);
            if (order == null)
                throw new NotFoundException("Đơn mua không tồn tại");

            order.Status = status;
            await _purchaseOrderRepo.UpdateAsync(order);

        }
        //Nhận hàng => kiểm đếm => xác nhận nhận hàng => cập nhật số lượng sản phẩm tương ứng
        public async Task<PurchanseOrder> OrderReceived(UpdatePurchaseOrderDto request)
        {
            var order = await _purchaseOrderRepo.GetAsync(request.Id);
            if (order == null)
            {
                throw new NotFoundException("Đơn mua không tồn tại");
            }
            foreach (var orderDetail in request.OrderDetails)
            {
                var detail = order.OrderDetails.FirstOrDefault(x => x.Id == orderDetail.Id);
                if (detail == null)
                    throw new NotFoundException($"Chi tiết đơn mua {orderDetail.Id} không tồn tại");
                detail.ActualReceivedQuantity = orderDetail.ActualReceivedQuantity;

            }

            order.ReceivedTo = request.ReceivedTo;
            order.Status = PurchaseOrderStatus.Received;
            await _purchaseOrderRepo.UpdateAsync(order);
            return order;

        }

        // map to DB object
        protected PurchanseOrder MapToPurchanse(CreatePurchaseOrderDto createPurchase)
        {
            var id = Guid.NewGuid();
            decimal? totalWeight = null;
            if (!string.IsNullOrWhiteSpace(createPurchase.TotalWeight)
                && decimal.TryParse(createPurchase.TotalWeight, out var parsedWeight))
            {
                totalWeight = parsedWeight;
            }

            var order = new PurchanseOrder()
            {
                Id = id,
                Code = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}-{++counter}",
                SupplierId = createPurchase.SupplierId,
                CreateDate = DateTime.UtcNow,
                ExpectedReceiveDate = createPurchase.ExpectedReceiveDate,
                ReceivingWarehouseId = createPurchase.WarehouseId,
                Status = PurchaseOrderStatus.Draft,
                Type = createPurchase.Type,
                TotalWeight = totalWeight,
            };

            foreach (var x in createPurchase.Products ?? new List<PurchanseOrderDetailDto>())
            {
                order.OrderDetails.Add(new PurchanseOrderDetail()
                {
                    Id = Guid.NewGuid(),
                    PurchanseOrderId = id,
                    ProductId = x.ProductId,
                    OrderedQuantity = x.OrderedQuantity,
                    UnitPrice = x.UnitPrice,
                    Type = "OrderItem",
                    ReceiveTime = DateTime.UtcNow,
                    InventoryId = Guid.NewGuid().ToString(),
                    Note = x.Note ?? string.Empty
                });
            }

            return order;
        }
    }
}
