using Inventory.Application.Inventory;
using Inventory.Application.Products.Services;
using Inventory.Application.PurchaseOrders;
using Inventory.Application.Suppliers;
using Inventory.Application.Users.Services;
using Inventory.Application.Warehouses;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IWarehouseService, WarehouseService>();

            return services;
        }
    }
}
