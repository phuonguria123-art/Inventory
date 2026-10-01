using Inventory.Application.Common.Models;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Application.PurchaseOrders.Dto.Receive;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders
{
    public interface IPurchaseOrderService
    {
        Task<PagedResult<PurchaseOrderDto>> GetPageAsync(int pageSize, int pageNumber, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, DateTime? createDate);
        Task<PurchaseOrderDto> GetDetailAsync(Guid purchaseOrderId);
        Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderDto purchaseOrder);
        Task UpdateAsync(Guid id, UpdatePurchaseOrderDto orderUpdate);
        Task UpdateStatusAsync(Guid id, PurchaseOrderStatus status);
        Task<PurchaseOrder> OrderReceived(ReceivePurchaseOrderRequestDto request, Guid userId);
        Task CancelAsync(Guid purchaseOrderId);
    }
}
