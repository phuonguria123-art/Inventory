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
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using static Inventory.Application.Abstraction.Caching.CacheKeys;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
    //Xuất kho thất bại do xuất quá tồn không thay đổi dữ liệu 
    [Fact]
    public async Task Issue_More_Than_Available_Quantity_Should_Return_Conflict_And_Not_Change_Stock()
    {
        //tạo mới kho và sản phẩm và tồn kho
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var reference = $"FAILED-ISSUE-{suffix}";
        var firstBatchNumber = $"BATCH-A-{suffix}";
        var secondBatchNumber = $"BATCH-B-{suffix}";
        //gọi api xuất kho với số lượng vượt quá tồn kho
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);
        await LoginAsStaffAsync(suffix);
        //API sử dụng một scope và DbContext khác. Sau request, context ban đầu vẫn đang track các entity đã seed nên có thể trả lại giá trị cũ, khiến assertion vượt qua dù database đã thay đổi.
        //Nên đóng scope seed rồi tạo scope mới để kiểm tra:
        using (var seedScope = _factory.Services.CreateScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.InventoryBalances.Add(new InventoryBalance
            {
                Id = inventoryId,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOnHand = 21,
                ReservedQuantity = 2
            });
            context.InventoryItems.AddRange(
                new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    InventoryId = inventoryId,
                    BatchNumber = firstBatchNumber,
                    Quantity = 8,
                    UnitCost = 10,
                    DateReceived = DateTime.UtcNow
                },
                  new InventoryItem
                  {
                      Id = Guid.NewGuid(),
                      InventoryId = inventoryId,
                      BatchNumber = secondBatchNumber,
                      Quantity = 13,
                      UnitCost = 10,
                      DateReceived = DateTime.UtcNow
                  }
                );
            await context.SaveChangesAsync();
        }

        var request = new
        {
            WarehouseId = warehouseId,
            Reference = reference,
            Products = new[]
            {
                new
                {
                    ProductId = productId,
                    Lots = new[]
                    {
                        new{BatchNumber = firstBatchNumber, Quantity = 8,},
                        new{BatchNumber = secondBatchNumber, Quantity = 12,}

                    }
                }
            }

        };
        var response = await _client.PostAsJsonAsync("/api/inventory/issue", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var assertionScope = _factory.Services.CreateScope();
        var assertionContext = assertionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventories = assertionContext.InventoryBalances
           .Single(inventory => inventory.Id == inventoryId);
        //kiểm tra inventoryItem có mã tồn kho tương ứng không
        var lots = assertionContext.InventoryItems
            .Where(item => item.InventoryId == inventoryId)
            .ToList();

        var transaction = assertionContext.InventoryTransactions.Where(x => x.Reference == reference).ToList();
        Assert.Equal(21, inventories.QuantityOnHand);
        Assert.Equal(2, inventories.ReservedQuantity);
        Assert.Equal(19, inventories.AvailableQuantity);
        Assert.Equal(21, lots.Sum(x => x.Quantity));
        Assert.Equal(8, lots.Single(x =>
          x.BatchNumber == firstBatchNumber).Quantity);
        Assert.Equal(13, lots.Single(x =>
            x.BatchNumber == secondBatchNumber).Quantity);

        Assert.Empty(transaction);
    }


    [Fact]
    //nhập hàng sau xuất hàng nên cập nhật invenrory và tạo mới transaction
    public async Task Receive_Then_Issue_Should_Update_Inventory_And_Create_Transactions()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);//tạo kho và sản phẩm
        await LoginAsStaffAsync(suffix);//đăng ký và đăng nhập user
        //gọi api nhập hàng
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
        //lấy response trả về khi nhập rồi kiểm tra giá trị
        var received = await receiveResponse.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(10, received?.Single().QuantityOnHand);
        Assert.Equal(10, received?.Single().AvailableQuantity);
        //xuất hàng
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
        //nhận data và kiểm tra giá trị
        var issued = await issueResponse.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(6, issued?.Single().QuantityOnHand);
        Assert.Equal(6, issued?.Single().AvailableQuantity);

        using var scope = _factory.Services.CreateScope();
        // lấy datacontext để truy vấn
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //truy vấn database hiện tại có đủ 2 bản sao nhập và xuất không
        Assert.Equal(2, context.InventoryTransactions.Count(transaction =>
            transaction.ProductId == productId && transaction.WarehouseId == warehouseId));
    }

    [Fact]
    //nhận nghiều sản phẩm và lô nên cập nhật toand bộ kho
    public async Task Receive_Multiple_Products_And_Lots_Should_Update_All_Stock_Atomically()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var reference = $"MULTI-RECEIPT-{suffix}";
        //tạo mới sản phẩm và kho
        await SeedProductAndWarehouseAsync(firstProductId, warehouseId, suffix);
        //tạo sản phẩm thứ 2
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
        //đăng nhập
        await LoginAsStaffAsync(suffix);
        //tạo dữ liệu có nhiều sản phẩm và nhiều lô
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
        //gọi api nhập hàng
        var response = await _client.PostAsJsonAsync("/api/inventory/receive", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //danh sách tồn kho nhận dduowjc xem có tạo được 2 tồn kho vì có 2 sản phẩm không
        var result = await response.Content.ReadFromJsonAsync<List<InventoryDto>>();
        Assert.Equal(2, result?.Count);
        //kiểm tra số lượng khả dụng có đúng với thông tin trên không
        Assert.Contains(result!, inventory =>
            inventory.ProductId == firstProductId && inventory.QuantityOnHand == 5);
        Assert.Contains(result!, inventory =>
            inventory.ProductId == secondProductId && inventory.QuantityOnHand == 4);
        //tiếp tục gửi requets có dữ liệu cũ xem api có báo lỗi conflix khôngs
        var repeatedResponse = await _client.PostAsJsonAsync("/api/inventory/receive", request);
        Assert.Equal(HttpStatusCode.Conflict, repeatedResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //lấy list inventory hiện tại theo kho
        var inventories = context.InventoryBalances
            .Where(inventory => inventory.WarehouseId == warehouseId)
            .ToList();
        var inventoryIds = inventories.Select(inventory => inventory.Id).ToList();
        //kiểm tra inventoryItem có mã tồn kho tương ứng không
        var lots = context.InventoryItems
            .Where(item => inventoryIds.Contains(item.InventoryId))
            .ToList();
        var transactions = context.InventoryTransactions
            .Where(transaction => transaction.Reference == reference)
            .ToList();

        Assert.Equal(3, lots.Count);
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, transaction => Assert.NotNull(transaction.InventoryItemId));
        //so sánh giá trị số lượng hợp lệ của tồn kho và tổng số lượng trong lô của tòn kho đó
        Assert.All(inventories, inventory => Assert.Equal(
            inventory.QuantityOnHand,
            lots.Where(item => item.InventoryId == inventory.Id).Sum(item => item.Quantity)));
    }

    [Fact]
    //xuất số lượng nhiều hơn số lượng có sẵn
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
    //chuyển nhiều tồn kho và tạo 2 lịch sử vì 1 kho xuất và 1 kho nhận
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
        //nhập kho => tạo tồn kho với sản phẩm và kho vừa tạo
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
        //chuyển tồn kho trên sang kho khác với số lượng là 4
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
        //lấy dbcontext từ cấu hình hiện tại
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //lấy ra tồn kho nguồn
        var source = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == sourceWarehouseId && inventory.ProductId == productId);
        //lấy tồn kho đích
        var destination = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == destinationWarehouseId && inventory.ProductId == productId);
        //kho nguồn xuất số lượng 4 nên còn 6
        Assert.Equal(6, source.QuantityOnHand);
        //kho đích nhận 4
        Assert.Equal(4, destination.QuantityOnHand);

        Assert.Equal(6, context.InventoryItems.Single(item =>
            item.InventoryId == source.Id && item.BatchNumber == $"BATCH-{suffix}").Quantity);
        Assert.Equal(4, context.InventoryItems.Single(item =>
            item.InventoryId == destination.Id && item.BatchNumber == $"BATCH-{suffix}").Quantity);
        //lấy danh sách lịch sử thay đổi số lượng trong kho theo mã đơn chuyển(reference) => có 2 thay đổi của kho đích và nguồn
        var transferTransactions = context.InventoryTransactions
            .Where(transaction => transaction.Reference == reference)
            .ToList();
        Assert.Equal(2, transferTransactions.Count);
        //2 thay đổi không được null
        Assert.All(transferTransactions, transaction => Assert.NotNull(transaction.InventoryItemId));
        //kiểm tra giá trị có đúng như logic không
        Assert.Contains(transferTransactions, transaction =>
            transaction.TransactionType == InventoryTransactionType.TransferOut &&
            transaction.WarehouseId == sourceWarehouseId);
        Assert.Contains(transferTransactions, transaction =>
            transaction.TransactionType == InventoryTransactionType.TransferIn &&
            transaction.WarehouseId == destinationWarehouseId);
    }

    [Fact]
    //chuyển kho khi không đủ tồn kho nên không có bất kỳ kho nào thay đổi
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
        //nhập hàng => tạo tồn kho với sản phẩm có số lượng là 3
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
        //chuyển kho với sản phẩm có số lượng là 4 nên kho không đủ hàng
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
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);//nhận statusCode lỗi

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //lấy ra tồn kho theo kho và sản phẩm
        var source = context.InventoryBalances.Single(inventory =>
            inventory.WarehouseId == sourceWarehouseId && inventory.ProductId == productId);
        //xem số lượng có sẵn có phải là 3 không
        Assert.Equal(3, source.QuantityOnHand);
        //do chyển kho thất bại nên không có kho nào thay đổi => không có kho nào chứa tồn phù hợp với kho và sản phẩm chuyển và cả mã đơn chuyển(reference)
        Assert.DoesNotContain(context.InventoryBalances, inventory =>
            inventory.WarehouseId == destinationWarehouseId && inventory.ProductId == productId);
        Assert.DoesNotContain(context.InventoryTransactions, transaction =>
            transaction.Reference == reference);
    }

    [Fact]
    //lấy danh sách lịch sử biến động tồn kho có lọc , đếm, xắp xếp
    public async Task GetTransactionsAsync_Should_Filter_Count_And_Order_Results()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var otherWarehouseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        //tạo 2 kho và 1 sản phẩm
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);
        await SeedWarehouseAsync(otherWarehouseId, $"other-{suffix}");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //tạo mới 3 transaction cho 2 kho vừa tạo
        context.InventoryTransactions.AddRange(
            CreateInventoryTransaction(warehouseId, productId, userId, $"OLDER-{suffix}", DateTime.UtcNow.AddMinutes(-1)),
            CreateInventoryTransaction(warehouseId, productId, userId, $"NEWER-{suffix}", DateTime.UtcNow),
            CreateInventoryTransaction(otherWarehouseId, productId, userId, $"OTHER-{suffix}", DateTime.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        //lấy danh sách lịch sử biến động theo 1 sản phẩm ở 1 kho, lấy 1 item mỗi trang
        var result = await service.GetTransactionsAsync(
            pageSize: 1,
            pageNumber: 1,
            warehouseId,
            productId,
            transactionType: null,
            createdByUserId: null,
            reference: null);
        //kiểm tra giá trị trả về từ api có 2 kết quả vì đã tạo 2 transaction tương ứng phía trên
        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal($"NEWER-{suffix}", result.Items.Single().Reference);
    }

    [Fact]
    //đặt trước thì phải tăng số lượng đặt và giảm số lượng khả dụng
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
        //thềm tồn kho với 1 sản phẩm có 2 lô , có sẵn là 10
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
        // giữ thêm 3 sản phẩm, ReservedQuantity tăng từ 2 lên 5
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
        //có sẵn vẫn là 10
        Assert.Equal(10, result.Single().QuantityOnHand);
        //giữ 5 => khả dụng = 5
        Assert.Equal(5, result.Single().ReservedQuantity);
        Assert.Equal(5, result.Single().AvailableQuantity);
        //kiểm tra thông tin giữ hàng trong bảng InventoryReservations
        var reservation = context.InventoryReservations.Single(item =>
            item.Reference == reference && item.InventoryId == inventoryId);
        Assert.Equal(InventoryReservationStatus.Active, reservation.Status);
        Assert.Equal(userId, reservation.CreatedByUserId);
        Assert.Equal(3, reservation.Quantity);
        //kiểm tra giữ hàng với số lượng 6 => lỗi vì chỉ còn 5 khả dụng
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
        //kiểm tra hủy giữ hàng với số lượng là 3 thì giữ hàng còn 5-3=2 và khả dụng là 5+3 = 8, trạng thái là đã hủy giữ hàng
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
        //tiếp tục giữ hàng với số lượng là 4 thì khả dụng còn 8-4 =4, giữ hàng là 2+4=6,
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
        //xuất hàng số lượng 4 từ đơn hàng vừa giữ số lượng 4 phía trên => giữ hàng là 6-4=2,có sẵn là 10-4=6
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
        //đối chiếu giá trị sau khi xuất hàng
        Assert.Equal(6, issued.Single().QuantityOnHand);
        Assert.Equal(2, issued.Single().ReservedQuantity);
        Assert.Equal(4, issued.Single().AvailableQuantity);
        Assert.Equal(6, context.InventoryItems
            .Where(item => item.InventoryId == inventoryId)
            .Sum(item => item.Quantity));
        //kiểm tra trạng thái bản ghi giữ hàng sau khi đã xuất thành công
        Assert.Equal(
            InventoryReservationStatus.Fulfilled,
            context.InventoryReservations.Single(item =>
                item.InventoryId == inventoryId &&
                item.Reference == fulfillmentReference).Status);
    }

    [Fact]
    //kiểm kê Lưu giữ các lô chưa kiểm đếm và cập nhật tổng số dựa trên phần chênh lệch
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
        //tạo mới kho và sản phẩm
        await SeedProductAndWarehouseAsync(productId, warehouseId, suffix);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //thêm tồn kho từ sản phẩm và kho vừa tạo
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
        //gọi api kiểm kê cho 1 lô của 1 sản phẩm tại 1 kho=> lô nào không kiểm tới thì không update
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
        //do kiểm kê lô adjustedBatchNumber từ 6 xuống còn 5 nên số lượng có sẵn lad 10-1=9
        Assert.Equal(9, result.Single().QuantityOnHand);
        //lô được kiểm kê thì update số lượng
        Assert.Equal(5, context.InventoryItems.Single(item =>
            item.InventoryId == inventoryId && item.BatchNumber == adjustedBatchNumber).Quantity);
        //lô không kiểm kê thì không thay đổi
        Assert.Equal(4, context.InventoryItems.Single(item =>
            item.InventoryId == inventoryId && item.BatchNumber == uncountedBatchNumber).Quantity);
        //update 1 lô nên 1 transaction được tạo với số lượng bị lệch là -1 nên có trạng thái là AdjustmentDecrease và số lượng lệch là 1 
        var transaction = context.InventoryTransactions.Single(item =>
            item.Reference == reference && item.ProductId == productId);
        Assert.Equal(InventoryTransactionType.AdjustmentDecrease, transaction.TransactionType);
        Assert.Equal(1, transaction.Quantity);
    }

    [Fact]
    //Các truy vấn hàng tồn kho cần thực hiện lọc, sắp xếp và phân loại hàng hóa.
    public async Task InventoryQueries_Should_Filter_Sort_And_Classify_Stock()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var warehouseId = Guid.NewGuid();
        var lowStockProductId = Guid.NewGuid();
        var excessProductId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //thêm kho
        context.Warehouses.Add(new Warehouse
        {
            Id = warehouseId,
            Name = $"Central Warehouse {suffix}",
            Code = $"CENTRAL-{suffix}",
            Phone = "0900000000",
            Address = "Test address",
            Capacity = 1000
        });
        //thêm sản 2 phẩm thừa và thiếu
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
        //thêm tồn kho cho 2 sản phẩm trên
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
        //gọi hàm lấy danh sách tồn kho
        var page = await service.GetPagedAsync(
            pageSize: 10,
            pageNumber: 1,
            warehouseId,
            productId: null,
            warehouseSearch: "CENTRAL",
            productSearch: suffix,
            sortBy: "availableQuantity",
            sortDescending: false);
        //kết quả của danh sách là có 2 sản phẩm vừa tạo xắp xếp tăng dân theo số lượng khả dụng
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(lowStockProductId, page.Items.First().ProductId);
        Assert.All(page.Items, item => Assert.Equal($"CENTRAL-{suffix}", item.WarehouseCode));
        Assert.Contains(page.Items, item => item.ProductCode == $"ALPHA-{suffix}");
        //lấy ra tồn kho theo kho và sản phẩm sắp hết hàng
        var detail = await service.GetByWarehouseAndProductAsync(warehouseId, lowStockProductId);
        Assert.Equal($"Central Warehouse {suffix}", detail.WarehouseName);
        Assert.Equal($"Alpha Product {suffix}", detail.ProductName);

        var lowStock = await service.GetLowStockAsync();
        var excessStock = await service.GetExcessStockAsync();
        //kiểm tra xem giá trị sắp hết và quá số lượng nhận về từ 2 api trên với 2sp có dữ liệu ban đầu tạo có đúng không
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
