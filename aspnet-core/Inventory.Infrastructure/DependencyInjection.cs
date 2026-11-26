using Inventory.Application.Abstraction.Caching;
using Inventory.Application.Interfaces;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Authentication;
using Inventory.Infrastructure.Caching;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("DBConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing.");
            var redisConnection =
                configuration["Redis:Configuration"]
                ?? throw new InvalidOperationException(
                    "Redis connection is missing.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddHealthChecks()
              .AddSqlServer(connectionString, name: "sqlserver", tags: ["ready"])
              .AddRedis(redisConnection, name: "redis", tags: ["ready"]);
            //  services.AddMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration =
                    redisConnection;

                options.InstanceName =
                    configuration["Redis:InstanceName"];
            });

            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<ICacheService, DistributedCacheService>();

            return services;
        }
    }
}