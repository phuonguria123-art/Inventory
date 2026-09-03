using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Inventories> Inventories { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchanseOrder> PurchanseOrders { get; set; }
        public DbSet<PurchanseOrderDetail> PurchanseOrdersDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product -> Supplier (one-to-many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasIndex(product => product.Code)
                .IsUnique();
            modelBuilder.Entity<Product>()
                .Property(product => product.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Warehouse>()
                .HasIndex(warehouse => warehouse.Code)
                .IsUnique();
            modelBuilder.Entity<Warehouse>()
                .Property(warehouse => warehouse.Capacity)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Supplier>()
                .HasIndex(supplier => supplier.Code)
                .IsUnique();
            modelBuilder.Entity<Supplier>()
                .Property(supplier => supplier.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Inventories>()
                .Property(inventory => inventory.UnitPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<InventoryItem>()
                .Property(item => item.UnitCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<PurchanseOrder>()
                .Property(order => order.Freight)
                .HasPrecision(18, 2);
            modelBuilder.Entity<PurchanseOrder>()
                .Property(order => order.TotalPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<PurchanseOrder>()
                .Property(order => order.TotalWeight)
                .HasPrecision(18, 2);
            modelBuilder.Entity<PurchanseOrderDetail>()
                .Property(detail => detail.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(x => new { x.UserId, x.RoleId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(x => new { x.RoleId, x.PermissionId });

            modelBuilder.Entity<Role>()
            .HasIndex(x => x.Name)
            .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(x => x.Code)
                .IsUnique();

            AuthorizationSeedData.Seed(modelBuilder);
        }

    }
}
