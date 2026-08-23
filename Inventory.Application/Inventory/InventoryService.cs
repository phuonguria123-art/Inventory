using Inventory.Application.Inventory.Dto;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Inventory
{
    public class InventoryService(IInventoryRepository _inventoryRepositories)
    {
        public async Task ChangeQuantityAsync(UpdateProductQuantityInput request)
        {
            var inventory = await _inventoryRepositories.GetAsync(request.WarehouseId, request.ProductId);
            if (inventory == null)
            {
                if (request.QUantity < 0)
                {
                    throw new Exception("Không đủ tồn kho");
                }
                await _inventoryRepositories.CreateAsync(new Inventories
                {
                    WarehouseId = request.WarehouseId,
                    ProductId = request.ProductId,
                    QuantityOnHand = request.QUantity,
                });
                return;

            }
            var newQuantity = inventory.QuantityOnHand + request.QUantity;
            if (newQuantity < 0) {
                throw new Exception("Không đủ tồn kho");
            }
            inventory.QuantityOnHand = newQuantity;
            await _inventoryRepositories.UpdateAsync(inventory);


        }
        //kiểm đếm kho
        public async Task InventoryCheck(InventoryCheckDto request)
        {
            foreach (var item in request.Products)
            {
                var inventory = await _inventoryRepositories.GetAsync(request.WarehouseId, item.ProductId);
                if (inventory == null)
                {
                    await _inventoryRepositories.CreateAsync(new Inventories
                    {
                        WarehouseId = request.WarehouseId,
                        ProductId = item.ProductId,
                        QuantityOnHand = item.ActualQuantity,
                    });
                    continue;
                }

                inventory.QuantityOnHand = item.ActualQuantity;
                await _inventoryRepositories.UpdateAsync(inventory);
            }
        }
    }
}

