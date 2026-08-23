using AutoMapper;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Entity;
using Inventory.Domain.Enums;
using Inventory.Domain.Interface;
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
        IProductRepository _productRepository,
        IMapper _mapper
        )
    {
        private static int counter = 0;
        // tạo mới đơn đặt hàng
        public async Task<PurchanseOrder> Create(CreatePurchaseOrderDto purchanseOrder)
        {
            //validate supplier,warehouse,product 
            if (!await _warehouseRepository.ExistsAsync(purchanseOrder.WarehouseId))
                throw new Exception("Warehouse không tồn tại");

            if (!await _supplierRepository.ExistsAsync(purchanseOrder.WarehouseId))
                throw new Exception("Supplier không tồn tại");
            if (purchanseOrder.Products.Count == 0)
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
            return null;

        }
        // cập nhật trạng thái Sent, confirm,shipping
        public async Task UpdateStatus(Guid id, PurchaseOrderStatus status)
        {
            var order = await _purchaseOrderRepo.GetAsync(id);
            order.Status = status;
            await _purchaseOrderRepo.UpdateAsync(order);

        }
        //Nhận hàng => kiểm đếm => xác nhận nhận hàng => cập nhật số lượng sản phẩm tương ứng
        public async Task<PurchanseOrder> OrderReceived(UpdatePurchaseOrderDto request)
        {
            var order = await _purchaseOrderRepo.GetAsync(request.Id);
            if (order == null)
            {
                return null;
            }
            foreach (var orderDetail in request.OrderDetails)
            {
                var detail = order.OrderDetails.FirstOrDefault(x => x.Id == orderDetail.Id);
                if (detail == null)
                    return null;
                detail.ActualReceivedQuantity = orderDetail.ActualReceivedQuantity;

            }

            order.ReceivedTo = request.ReceivedTo;
            order.Status = PurchaseOrderStatus.Received;
            await _purchaseOrderRepo.UpdateAsync(order);
            return order;

        }
        protected PurchanseOrder MapToPurchanse(CreatePurchaseOrderDto createPurchase)
        {
            Guid id = Guid.NewGuid();
            var order = new PurchanseOrder()
            {
                Id = id,
                Code = "P" + counter++,
                SupplierId = createPurchase.SupplierId,
                CreateDate = DateTime.UtcNow,
                ExpectedReceiveDate = createPurchase.ExpectedReceiveDate,
                ReceivingWarehouseId = createPurchase.WarehouseId,
                Freight = createPurchase.Freight,
                Status = PurchaseOrderStatus.Draft,
                TotalWeight = createPurchase.TotalWeight,

            };
            decimal orderTotal = 0;
            if (createPurchase.Products?.Count <= 0)
            {
                return null;
            }
            foreach (var item in createPurchase.Products)
            {
                order.OrderDetails.Add(new PurchanseOrderDetail()
                {
                    PurchanseOrderId = id,
                    ProductId = item.ProductId,
                    UnitPrice = item.UnitPrice,
                    Id = Guid.NewGuid(),
                    OrderedQuantity = item.OrderedQuantity,

                });
                orderTotal += (item.OrderedQuantity * item.UnitPrice);
            }
            order.TotalPrice = orderTotal;
            return order;
        }
    }
}
