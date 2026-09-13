using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Inventory.Dto.Adjust;
using Inventory.Application.Inventory.Dto.Issue;
using Inventory.Application.Inventory.Dto.Receive;
using Inventory.Application.Inventory.Dto.Reservation;
using Inventory.Application.Users.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.Api.Tests;

public sealed class InventoryFlowTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;
    private readonly HttpClient _client;

    public InventoryFlowTests(InventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Receive_Then_Issue_Should_Update_Inventory_And_Create_Transactions()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);
        await LoginAsStaffAsync(suffix);

        var receiveResponse = await _client.PostAsJsonAsync("/api/inventory/receive", new
        {
            WarehouseId = warehouseId,
            Reference = $"RECEIPT-{suffix}",
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new
                        {
                            BatchNumber = $"BATCH-{suffix}",
                            Quantity = 10,
                            UnitCost = 10m
                        }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, receiveResponse.StatusCode);

        var received = await receiveResponse.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(10, received?.Single().QuantityOnHand);
        Assert.Equal(10, received?.Single().AvailableQuantity);

        var issueResponse = await _client.PostAsJsonAsync("/api/inventory/issue", new
        {
            WarehouseId = warehouseId,
            Reference = $"ISSUE-{suffix}",
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new { BatchNumber = $"BATCH-{suffix}", Quantity = 4 }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, issueResponse.StatusCode);

        var issued = await issueResponse.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(6, issued?.Single().QuantityOnHand);
        Assert.Equal(6, issued?.Single().AvailableQuantity);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.InventoryTransactions.Count(transaction =>
            transaction.ProductId == productId && transaction.WarehouseId == warehouseId));
    }

    [Fact]
    public async Task Receive_Multiple_Products_And_Lots_Should_Update_All_Stock_Atomically()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var reference = $"MULTI-RECEIPT-{suffix}";

        await SeedProductAndWarehouseAsync(firstProductId, warehouseId, suffix);
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedContext.Products.Add(new Product
            {
                Id = secondProductId,
                Name = $"Second Product {suffix}",
                Code = $"P2-{suffix}",
                UnitPrice = 20
            });
            await seedContext.SaveChangesAsync();
        }

        await LoginAsStaffAsync(suffix);

        var request = new
        {
            WarehouseId = warehouseId,
            Reference = reference,
            Products = new object[]
            {
                new
                {
                    ProductId = firstProductId,
                    Lots = new[]
                    {
                        new { BatchNumber = $"BATCH-A-{suffix}", Quantity = 3, UnitCost = 10m },
                        new { BatchNumber = $"BATCH-B-{suffix}", Quantity = 2, UnitCost = 10m }
                    }
                },
                new
                {
                    ProductId = secondProductId,
                    Lots = new[]
                    {
                        new { BatchNumber = $"BATCH-C-{suffix}", Quantity = 4, UnitCost = 20m }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/inventory/receive", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(2, result?.Count);
        Assert.Contains(result!, inventory =>
            inventory.ProductId == firstProductId && inventory.QuantityOnHand == 5);
        Assert.Contains(result!, inventory =>
            inventory.ProductId == secondProductId && inventory.QuantityOnHand == 4);

        var repeatedResponse = await _client.PostAsJsonAsync("/api/inventory/receive", request);
        Assert.Equal(HttpStatusCode.Conflict, repeatedResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventories = context.InventoryBalances
            .Where(inventory => inventory.WarehouseId == warehouseId)
            .ToList();
        var inventoryIds = inventories.Select(inventory => inventory.Id).ToList();
        var lots = context.InventoryItems
            .Where(item => inventoryIds.Contains(item.InventoryId))
            .ToList();
        var transactions = context.InventoryTransactions
            .Where(transaction => transaction.Reference == reference)
            .ToList();

        Assert.Equal(3, lots.Count);
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, transaction => Assert.NotNull(transaction.InventoryItemId));
        Assert.All(inventories, inventory => Assert.Equal(
            inventory.QuantityOnHand,
            lots.Where(item => item.InventoryId == inventory.Id).Sum(item => item.Quantity)));
    }

    [Fact]
    public async Task Issue_More_Than_Available_Should_Return_Conflict()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);
        await LoginAsStaffAsync(suffix);

        var receiveResponse = await _client.PostAsJsonAsync("/api/inventory/receive", new
        {
            WarehouseId = warehouseId,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new
                        {
                            BatchNumber = $"BATCH-{suffix}",
                            Quantity = 1,
                            UnitCost = 10m
                        }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, receiveResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/inventory/issue", new
        {
            WarehouseId = warehouseId,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new { BatchNumber = $"BATCH-{suffix}", Quantity = 2 }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_Should_Move_Stock_And_Create_A_Paired_History()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var sourceWarehouseId = Guid.NewGuid();
        var destinationWarehouseId = Guid.NewGuid();
        var reference = $"TRANSFER-{suffix}";
        await SeedProductAndWarehouseAsync(productId, sourceWarehouseId, suffix);
        await SeedWarehouseAsync(destinationWarehouseId, $"destination-{suffix}");
        await LoginAsStaffAsync(suffix);

        var receiveResponse = await _client.PostAsJsonAsync("/api/inventory/receive", new
        {
            WarehouseId = sourceWarehouseId,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new
                        {
                            BatchNumber = $"BATCH-{suffix}",
                            Quantity = 10,
                            UnitCost = 10m
                        }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, receiveResponse.StatusCode);

        var transferResponse = await _client.PostAsJsonAsync("/api/inventory/transfer", new
        {
            WarehouseFromId = sourceWarehouseId,
            WarehouseToId = destinationWarehouseId,
            Reference = reference,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new
                        {
                            BatchNumber = $"BATCH-{suffix}",
                            Quantity = 4,
                            Location = "DEST-A"
                        }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.NoContent, transferResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var source = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == sourceWarehouseId && inventory.ProductId == productId);
        var destination = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == destinationWarehouseId && inventory.ProductId == productId);

        Assert.Equal(6, source.QuantityOnHand);
        Assert.Equal(4, destination.QuantityOnHand);
        Assert.Equal(6, context.InventoryItems.Single(item =>
            item.InventoryId == source.Id && item.BatchNumber == $"BATCH-{suffix}").Quantity);
        Assert.Equal(4, context.InventoryItems.Single(item =>
            item.InventoryId == destination.Id && item.BatchNumber == $"BATCH-{suffix}").Quantity);

        var transferTransactions = context.InventoryTransactions
            .Where(transaction => transaction.Reference == reference)
            .ToList();
        Assert.Equal(2, transferTransactions.Count);
        Assert.All(transferTransactions, transaction => Assert.NotNull(transaction.InventoryItemId));
        Assert.Contains(transferTransactions, transaction =>
            transaction.TransactionType == InventoryTransactionType.TransferOut &&
            transaction.WarehouseId == sourceWarehouseId);
        Assert.Contains(transferTransactions, transaction =>
            transaction.TransactionType == InventoryTransactionType.TransferIn &&
            transaction.WarehouseId == destinationWarehouseId);
    }

    [Fact]
    public async Task Transfer_With_Insufficient_Stock_Should_Not_Change_Either_Warehouse()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var sourceWarehouseId = Guid.NewGuid();
        var destinationWarehouseId = Guid.NewGuid();
        var reference = $"FAILED-{suffix}";
        await SeedProductAndWarehouseAsync(productId, sourceWarehouseId, suffix);
        await SeedWarehouseAsync(destinationWarehouseId, $"destination-{suffix}");
        await LoginAsStaffAsync(suffix);

        await _client.PostAsJsonAsync("/api/inventory/receive", new
        {
            WarehouseId = sourceWarehouseId,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new
                        {
                            BatchNumber = $"BATCH-{suffix}",
                            Quantity = 3,
                            UnitCost = 10m
                        }
                    }
                }
            }
        });

        var response = await _client.PostAsJsonAsync("/api/inventory/transfer", new
        {
            WarehouseFromId = sourceWarehouseId,
            WarehouseToId = destinationWarehouseId,
            Reference = reference,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new { BatchNumber = $"BATCH-{suffix}", Quantity = 4 }
                    }
                }
            }
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var source = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == sourceWarehouseId && inventory.ProductId == productId);

        Assert.Equal(3, source.QuantityOnHand);
        Assert.DoesNotContain(context.InventoryBalances, inventory =>
            inventory.WarehouseId == destinationWarehouseId && inventory.ProductId == productId);
        Assert.DoesNotContain(context.InventoryTransactions, transaction =>
            transaction.Reference == reference);
    }

    [Fact]
    public async Task GetTransactionsAsync_Should_Filter_Count_And_Order_Results()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var otherWarehouseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);
        await SeedWarehouseAsync(otherWarehouseId, $"other-{suffix}");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.InventoryTransactions.AddRange(
            CreateInventoryTransaction(warehouseId, productId, userId, $"OLDER-{suffix}", DateTime.UtcNow.AddMinutes(-1)),
            CreateInventoryTransaction(warehouseId, productId, userId, $"NEWER-{suffix}", DateTime.UtcNow),
            CreateInventoryTransaction(otherWarehouseId, productId, userId, $"OTHER-{suffix}", DateTime.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var result = await service.GetTransactionsAsync(
            pageSize: 1,
            pageNumber: 1,
            warehouseId,
            productId,
            transactionType: null,
            createdByUserId: null,
            reference: null);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal($"NEWER-{suffix}", result.Items.Single().Reference);
    }

    [Fact]
    public async Task Reservation_Should_Increase_Reserved_Quantity_And_Decrease_Available_Quantity()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var firstBatchNumber = $"BATCH-A-{suffix}";
        var secondBatchNumber = $"BATCH-B-{suffix}";
        var userId = Guid.NewGuid();
        var reference = $"ORDER-{suffix}";
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.InventoryBalances.Add(new InventoryBalance
        {
            Id = inventoryId,
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityOnHand = 10,
            ReservedQuantity = 2
        });
        context.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                BatchNumber = firstBatchNumber,
                Quantity = 6,
                UnitCost = 10,
                DateReceived = DateTime.UtcNow,
                Location = "TEST-A"
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                BatchNumber = secondBatchNumber,
                Quantity = 4,
                UnitCost = 10,
                DateReceived = DateTime.UtcNow,
                Location = "TEST-B"
            });
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var result = await service.ReserveAsync(new ReserveInventoryRequestDto
        {
            WarehouseId = warehouseId,
            Reference = reference,
            Products =
            [
                new ReserveProductDto
                {
                    ProductId = productId,
                    Quantity = 3
                }
            ]
        }, userId);

        Assert.Equal(10, result.Single().QuantityOnHand);
        Assert.Equal(5, result.Single().ReservedQuantity);
        Assert.Equal(5, result.Single().AvailableQuantity);
        var reservation = context.InventoryReservations.Single(item =>
            item.Reference == reference && item.InventoryId == inventoryId);
        Assert.Equal(InventoryReservationStatus.Active, reservation.Status);
        Assert.Equal(userId, reservation.CreatedByUserId);
        Assert.Equal(3, reservation.Quantity);

        await Assert.ThrowsAsync<ConflictException>(() => service.ReserveAsync(
            new ReserveInventoryRequestDto
            {
                WarehouseId = warehouseId,
                Reference = $"TOO-MUCH-{suffix}",
                Products =
                [
                    new ReserveProductDto
                    {
                        ProductId = productId,
                        Quantity = 6
                    }
                ]
            },
            userId));

        var released = await service.ReserveAsync(new ReserveInventoryRequestDto
        {
            WarehouseId = warehouseId,
            Reference = reference,
            IsCancellation = true,
            Products =
            [
                new ReserveProductDto
                {
                    ProductId = productId,
                    Quantity = 3
                }
            ]
        }, userId);

        Assert.Equal(2, released.Single().ReservedQuantity);
        Assert.Equal(8, released.Single().AvailableQuantity);
        Assert.Equal(InventoryReservationStatus.Released, reservation.Status);

        var fulfillmentReference = $"FULFILL-{suffix}";
        await service.ReserveAsync(new ReserveInventoryRequestDto
        {
            WarehouseId = warehouseId,
            Reference = fulfillmentReference,
            Products =
            [
                new ReserveProductDto
                {
                    ProductId = productId,
                    Quantity = 4
                }
            ]
        }, userId);

        var issued = await service.IssueAsync(new IssueInventoryRequestDto
        {
            WarehouseId = warehouseId,
            Reference = fulfillmentReference,
            Products =
            [
                new IssueProductDto
                {
                    ProductId = productId,
                    Lots =
                    [
                        new IssueLotDto
                        {
                            BatchNumber = firstBatchNumber,
                            Quantity = 2
                        },
                        new IssueLotDto
                        {
                            BatchNumber = secondBatchNumber,
                            Quantity = 2
                        }
                    ]
                }
            ]
        }, userId);

        Assert.Equal(6, issued.Single().QuantityOnHand);
        Assert.Equal(2, issued.Single().ReservedQuantity);
        Assert.Equal(4, issued.Single().AvailableQuantity);
        Assert.Equal(6, context.InventoryItems
            .Where(item => item.InventoryId == inventoryId)
            .Sum(item => item.Quantity));
        Assert.Equal(
            InventoryReservationStatus.Fulfilled,
            context.InventoryReservations.Single(item =>
                item.InventoryId == inventoryId &&
                item.Reference == fulfillmentReference).Status);
    }

    [Fact]
    public async Task Adjusting_One_Lot_Should_Preserve_Uncounted_Lots_And_Update_Total_By_Difference()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var adjustedBatchNumber = $"COUNTED-{suffix}";
        var uncountedBatchNumber = $"UNCOUNTED-{suffix}";
        var reference = $"ADJUST-{suffix}";
        var userId = Guid.NewGuid();
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.InventoryBalances.Add(new InventoryBalance
        {
            Id = inventoryId,
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityOnHand = 10,
            ReservedQuantity = 2
        });
        context.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                BatchNumber = adjustedBatchNumber,
                Quantity = 6,
                UnitCost = 10,
                DateReceived = DateTime.UtcNow,
                Location = "TEST-A"
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                BatchNumber = uncountedBatchNumber,
                Quantity = 4,
                UnitCost = 10,
                DateReceived = DateTime.UtcNow,
                Location = "TEST-B"
            });
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var result = await service.AdjustAsync(new AdjustInventoryRequestDto
        {
            WarehouseId = warehouseId,
            Reference = reference,
            Notes = "Kiểm kê một phần",
            Products =
            [
                new AdjustProductDto
                {
                    ProductId = productId,
                    Lots =
                    [
                        new AdjustLotDto
                        {
                            BatchNumber = adjustedBatchNumber,
                            ActualQuantity = 5
                        }
                    ]
                }
            ]
        }, userId);

        Assert.Equal(9, result.Single().QuantityOnHand);
        Assert.Equal(5, context.InventoryItems.Single(item =>
            item.InventoryId == inventoryId && item.BatchNumber == adjustedBatchNumber).Quantity);
        Assert.Equal(4, context.InventoryItems.Single(item =>
            item.InventoryId == inventoryId && item.BatchNumber == uncountedBatchNumber).Quantity);

        var transaction = context.InventoryTransactions.Single(item =>
            item.Reference == reference && item.ProductId == productId);
        Assert.Equal(InventoryTransactionType.AdjustmentDecrease, transaction.TransactionType);
        Assert.Equal(1, transaction.Quantity);
    }

    [Fact]
    public async Task InventoryQueries_Should_Filter_Sort_And_Classify_Stock()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var warehouseId = Guid.NewGuid();
        var lowStockProductId = Guid.NewGuid();
        var excessProductId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Warehouses.Add(new Warehouse
        {
            Id = warehouseId,
            Name = $"Central Warehouse {suffix}",
            Code = $"CENTRAL-{suffix}",
            Phone = "0900000000",
            Address = "Test address",
            Capacity = 1000
        });
        context.Products.AddRange(
            new Product
            {
                Id = lowStockProductId,
                Name = $"Alpha Product {suffix}",
                Code = $"ALPHA-{suffix}",
                UnitPrice = 10
            },
            new Product
            {
                Id = excessProductId,
                Name = $"Beta Product {suffix}",
                Code = $"BETA-{suffix}",
                UnitPrice = 10
            });
        context.InventoryBalances.AddRange(
            new InventoryBalance
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                ProductId = lowStockProductId,
                QuantityOnHand = 10,
                ReservedQuantity = 8,
                MinStock = 3,
                MaxStock = 20
            },
            new InventoryBalance
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                ProductId = excessProductId,
                QuantityOnHand = 30,
                ReservedQuantity = 0,
                MinStock = 5,
                MaxStock = 20
            });
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var page = await service.GetPagedAsync(
            pageSize: 10,
            pageNumber: 1,
            warehouseId,
            productId: null,
            warehouseSearch: "CENTRAL",
            productSearch: suffix,
            sortBy: "availableQuantity",
            sortDescending: false);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(lowStockProductId, page.Items.First().ProductId);
        Assert.All(page.Items, item => Assert.Equal($"CENTRAL-{suffix}", item.WarehouseCode));
        Assert.Contains(page.Items, item => item.ProductCode == $"ALPHA-{suffix}");

        var detail = await service.GetByWarehouseAndProductAsync(warehouseId, lowStockProductId);
        Assert.Equal($"Central Warehouse {suffix}", detail.WarehouseName);
        Assert.Equal($"Alpha Product {suffix}", detail.ProductName);

        var lowStock = await service.GetLowStockAsync();
        var excessStock = await service.GetExcessStockAsync();
        Assert.Contains(lowStock, item => item.ProductId == lowStockProductId);
        Assert.Contains(excessStock, item => item.ProductId == excessProductId);
        Assert.DoesNotContain(excessStock, item => item.ProductId == lowStockProductId);
    }

    private async Task LoginAsStaffAsync(string suffix)
    {
        const string password = "StrongPassword!123";
        var username = $"inventory-{suffix}";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"inventory-{suffix}@example.com",
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });
        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private async Task SeedProductAndWarehouseAsync(Guid productId, Guid warehouseId, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Products.Add(new Product
        {
            Id = productId,
            Name = $"Product {suffix}",
            Code = $"P-{suffix}",
            UnitPrice = 10
        });
        context.Warehouses.Add(new Warehouse
        {
            Id = warehouseId,
            Name = $"Warehouse {suffix}",
            Code = $"W-{suffix}",
            Phone = "0900000000",
            Address = "Test address",
            Capacity = 1000
        });

        await context.SaveChangesAsync();
    }

    private async Task SeedWarehouseAsync(Guid warehouseId, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Warehouses.Add(new Warehouse
        {
            Id = warehouseId,
            Name = $"Warehouse {suffix}",
            Code = $"W-{suffix}",
            Phone = "0900000000",
            Address = "Test address",
            Capacity = 1000
        });
        await context.SaveChangesAsync();
    }

    private static InventoryTransaction CreateInventoryTransaction(
        Guid warehouseId,
        Guid productId,
        Guid userId,
        string reference,
        DateTime transactionDate) => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductId = productId,
            CreatedByUserId = userId,
            TransactionType = InventoryTransactionType.Receipt,
            Quantity = 1,
            Reference = reference,
            TransactionDate = transactionDate
        };
}
