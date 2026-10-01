using Inventory.Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Inventory.Api.Tests;

public sealed class InventoryApiFactory : WebApplicationFactory<Inventory.Api.Program>
{
    private readonly string _databaseName = $"inventory-tests-{Guid.NewGuid()}";

    public InventoryApiFactory()
    {
        // Program validates the key before WebApplicationFactory applies its
        // ConfigureAppConfiguration callback, so provide the test-only key up front.
        Environment.SetEnvironmentVariable(
            "AppSettings__Token",
            "integration-test-signing-key-with-at-least-32-characters");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:Issuer"] = "InventoryTests",
                ["AppSettings:Audience"] = "InventoryTests",
                ["AppSettings:AccessTokenMinutes"] = "15"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Database.EnsureCreated();
        });
    }
}
