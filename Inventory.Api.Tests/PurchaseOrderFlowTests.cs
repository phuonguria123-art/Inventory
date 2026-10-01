using Inventory.Application.PurchaseOrders;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Application.PurchaseOrders.Dto.Receive;
using Inventory.Application.PurrchanseOrderDetail.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.Api.Tests;

public sealed class PurchaseOrderFlowTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;

    public PurchaseOrderFlowTests(InventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_Draft_Should_Add_Update_Delete_Details_And_Recalculate_Total()
    {
        var data = await SeedPurchaseOrderDependenciesAsync();

        Guid orderId;
        Guid updatedDetailId;
        Guid removedDetailId;

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IPurchaseOrderService>();
            var order = await service.CreateAsync(new CreatePurchaseOrderDto
            {
                SupplierId = data.SupplierId,
                ReceivingWarehouseId = data.WarehouseId,
                ExpectedReceiveDate = DateTime.UtcNow.Date.AddDays(2),
                Freight = 5m,
                TotalWeight = 20m,
                Products =
                [
                    new PurchaseOrderDetailDto
                    {
                        ProductId = data.FirstProductId,
                        OrderedQuantity = 2,
                        UnitPrice = 10m,
                        Note = "Dòng sẽ được sửa"
                    },
                    new PurchaseOrderDetailDto
                    {
                        ProductId = data.SecondProductId,
                        OrderedQuantity = 1,
                        UnitPrice = 20m,
                        Note = "Dòng sẽ bị xóa"
                    }
                ]
            });

            orderId = order.Id;
            updatedDetailId = order.OrderDetails.Single(detail =>
                detail.ProductId == data.FirstProductId).Id;
            removedDetailId = order.OrderDetails.Single(detail =>
                detail.ProductId == data.SecondProductId).Id;

            await service.UpdateAsync(order.Id, new UpdatePurchaseOrderDto
            {
                SupplierId = data.SupplierId,
                ReceivingWarehouseId = data.WarehouseId,
                ExpectedReceiveDate = DateTime.UtcNow.Date.AddDays(3),
                Freight = 7m,
                TotalWeight = 30m,
                Type = "Restock",
                OrderDetails =
                [
                    new UpdatePurchaseOrderDetailDto
                    {
                        Id = updatedDetailId,
                        ProductId = data.FirstProductId,
                        OrderedQuantity = 3,
                        UnitPrice = 11m,
                        Note = "Đã sửa"
                    },
                    new UpdatePurchaseOrderDetailDto
                    {
                        ProductId = data.ThirdProductId,
                        OrderedQuantity = 4,
                        UnitPrice = 7m,
                        Note = "Dòng mới"
                    }
                ]
            });
        }

        using var assertionScope = _factory.Services.CreateScope();
        var context = assertionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedOrder = await context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.OrderDetails)
            .SingleAsync(order => order.Id == orderId);

        Assert.Equal(PurchaseOrderStatus.Draft, updatedOrder.Status);
        Assert.Equal(7m, updatedOrder.Freight);
        Assert.Equal(30m, updatedOrder.TotalWeight);
        Assert.Equal(68m, updatedOrder.TotalPrice);
        Assert.Equal(2, updatedOrder.OrderDetails.Count);
        Assert.DoesNotContain(updatedOrder.OrderDetails, detail => detail.Id == removedDetailId);
        Assert.Contains(updatedOrder.OrderDetails, detail =>
            detail.Id == updatedDetailId &&
            detail.ProductId == data.FirstProductId &&
            detail.OrderedQuantity == 3 &&
            detail.UnitPrice == 11m &&
            detail.Note == "Đã sửa");
        Assert.Contains(updatedOrder.OrderDetails, detail =>
            detail.ProductId == data.ThirdProductId &&
            detail.OrderedQuantity == 4 &&
            detail.UnitPrice == 7m &&
            detail.Note == "Dòng mới");
    }

    [Fact]
    public async Task Create_Send_Receive_Should_Update_Stock_Lots_Transactions_And_Block_Reprocessing()
    {
        var data = await SeedPurchaseOrderDependenciesAsync();
        var userId = Guid.NewGuid();
        var firstBatch = $"PO-BATCH-A-{data.Suffix}";
        var secondBatch = $"PO-BATCH-B-{data.Suffix}";

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Users.Add(new User
        {
            Id = userId,
            Username = $"receiver-{data.Suffix}",
            Email = $"receiver-{data.Suffix}@example.com",
            PasswordHash = "test-only-hash"
        });
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IPurchaseOrderService>();
        var order = await service.CreateAsync(new CreatePurchaseOrderDto
        {
            SupplierId = data.SupplierId,
            ReceivingWarehouseId = data.WarehouseId,
            ExpectedReceiveDate = DateTime.UtcNow.Date.AddDays(2),
            Freight = 5m,
            Products =
            [
                new PurchaseOrderDetailDto
                {
                    ProductId = data.FirstProductId,
                    OrderedQuantity = 5,
                    UnitPrice = 10m
                },
                new PurchaseOrderDetailDto
                {
                    ProductId = data.SecondProductId,
                    OrderedQuantity = 3,
                    UnitPrice = 20m
                }
            ]
        });

        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
        Assert.Equal(115m, order.TotalPrice);

        await service.UpdateStatusAsync(order.Id, PurchaseOrderStatus.Sent);
        await service.UpdateStatusAsync(order.Id, PurchaseOrderStatus.Confirmed);
        await service.UpdateStatusAsync(order.Id, PurchaseOrderStatus.Shipping);

        var firstDetail = order.OrderDetails.Single(detail =>
            detail.ProductId == data.FirstProductId);
        var secondDetail = order.OrderDetails.Single(detail =>
            detail.ProductId == data.SecondProductId);
        var receiveRequest = new ReceivePurchaseOrderRequestDto
        {
            PurchaseOrderId = order.Id,
            OrderDetails =
            [
                new ReceivePurchaseOrderProductDto
                {
                    PurchaseOrderDetailId = firstDetail.Id,
                    Lots =
                    [
                        new ReceivePurchaseOrderLotDto
                        {
                            BatchNumber = firstBatch,
                            ReceivedQuantity = 5,
                            UnitCost = 9m,
                            Location = "A-01"
                        }
                    ]
                },
                new ReceivePurchaseOrderProductDto
                {
                    PurchaseOrderDetailId = secondDetail.Id,
                    Lots =
                    [
                        new ReceivePurchaseOrderLotDto
                        {
                            BatchNumber = secondBatch,
                            ReceivedQuantity = 3,
                            UnitCost = 18m,
                            Location = "B-01"
                        }
                    ]
                }
            ]
        };

        var receivedOrder = await service.OrderReceived(receiveRequest, userId);

        Assert.Equal(PurchaseOrderStatus.Received, receivedOrder.Status);
        Assert.NotNull(receivedOrder.ReceivedDate);
        Assert.Equal(userId.ToString(), receivedOrder.ReceivedTo);
        Assert.All(receivedOrder.OrderDetails, detail =>
            Assert.Equal(detail.OrderedQuantity, detail.ActualReceivedQuantity));

        var balances = await context.InventoryBalances
            .Where(balance => balance.WarehouseId == data.WarehouseId)
            .ToListAsync();
        Assert.Contains(balances, balance =>
            balance.ProductId == data.FirstProductId && balance.QuantityOnHand == 5);
        Assert.Contains(balances, balance =>
            balance.ProductId == data.SecondProductId && balance.QuantityOnHand == 3);

        var balanceIds = balances.Select(balance => balance.Id).ToList();
        var lots = await context.InventoryItems
            .Where(item => balanceIds.Contains(item.InventoryId))
            .ToListAsync();
        Assert.Contains(lots, lot => lot.BatchNumber == firstBatch && lot.Quantity == 5);
        Assert.Contains(lots, lot => lot.BatchNumber == secondBatch && lot.Quantity == 3);

        var transactions = await context.InventoryTransactions
            .Where(transaction => transaction.Reference == order.Code)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, transaction =>
        {
            Assert.Equal(InventoryTransactionType.Receipt, transaction.TransactionType);
            Assert.Equal(userId, transaction.CreatedByUserId);
            Assert.NotNull(transaction.InventoryItemId);
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.OrderReceived(receiveRequest, userId));
        Assert.Equal(2, await context.InventoryTransactions.CountAsync(transaction =>
            transaction.Reference == order.Code));
    }

    private async Task<PurchaseOrderTestData> SeedPurchaseOrderDependenciesAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var data = new PurchaseOrderTestData(
            suffix,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Suppliers.Add(new Supplier
        {
            Id = data.SupplierId,
            Name = $"Supplier {suffix}",
            Code = $"SUP-{suffix}",
            Address = "Test address",
            Phone = "0900000000",
            IsActive = true
        });
        context.Warehouses.Add(new Warehouse
        {
            Id = data.WarehouseId,
            Name = $"Warehouse {suffix}",
            Code = $"WH-{suffix}",
            Address = "Test address",
            Phone = "0900000000",
            Capacity = 1000
        });
        context.Products.AddRange(
            CreateProduct(data.FirstProductId, $"P1-{suffix}", data.SupplierId),
            CreateProduct(data.SecondProductId, $"P2-{suffix}", data.SupplierId),
            CreateProduct(data.ThirdProductId, $"P3-{suffix}", data.SupplierId));
        await context.SaveChangesAsync();

        return data;
    }

    private static Product CreateProduct(Guid id, string code, Guid supplierId) => new()
    {
        Id = id,
        Name = $"Product {code}",
        Code = code,
        UnitPrice = 10m,
        SupplierId = supplierId
    };

    private sealed record PurchaseOrderTestData(
        string Suffix,
        Guid SupplierId,
        Guid WarehouseId,
        Guid FirstProductId,
        Guid SecondProductId,
        Guid ThirdProductId);
}
