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
        public DbSet<InventoryReservation> InventoryReservations { get; set; }
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
                .HasIndex(inventory => new { inventory.WarehouseId, inventory.ProductId })
                .IsUnique();
            modelBuilder.Entity<Inventories>()
                .HasOne(inventory => inventory.Warehouse)
                .WithMany()
                .HasForeignKey(inventory => inventory.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Inventories>()
                .HasOne(inventory => inventory.Product)
                .WithMany()
                .HasForeignKey(inventory => inventory.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Inventories>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Inventories_QuantityOnHand", "[QuantityOnHand] >= 0");
                    table.HasCheckConstraint("CK_Inventories_ReservedQuantity", "[ReservedQuantity] >= 0 AND [ReservedQuantity] <= [QuantityOnHand]");
                });

            modelBuilder.Entity<InventoryItem>()
                .Property(item => item.UnitCost)
                .HasPrecision(18, 2);
            //modelBuilder.Entity<InventoryItem>()
            //    .HasOne(item => item.Warehouse)
            //    .WithMany()
            //    .HasForeignKey(item => item.WarehouseId)
            //    .OnDelete(DeleteBehavior.Restrict);
            //modelBuilder.Entity<InventoryItem>()
            //    .HasOne(item => item.Product)
            //    .WithMany()
            //    .HasForeignKey(item => item.ProductId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryTransaction>()
                .Property(transaction => transaction.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(32);
            modelBuilder.Entity<InventoryTransaction>()
                .Property(transaction => transaction.Reference)
                .HasMaxLength(100);
            modelBuilder.Entity<InventoryTransaction>()
                .HasIndex(transaction => transaction.Reference);
            modelBuilder.Entity<InventoryTransaction>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_InventoryTransactions_Quantity",
                    "[Quantity] > 0"));
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(transaction => transaction.Warehouse)
                .WithMany()
                .HasForeignKey(transaction => transaction.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(transaction => transaction.Product)
                .WithMany()
                .HasForeignKey(transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(transaction => transaction.InventoryItem)
                .WithMany()
                .HasForeignKey(transaction => transaction.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(transaction => transaction.CreatedByUser)
                .WithMany(user => user.InventoryTransactions)
                .HasForeignKey(transaction => transaction.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryReservation>()
                .Property(reservation => reservation.Reference)
                .HasMaxLength(100)
                .IsRequired();
            modelBuilder.Entity<InventoryReservation>()
                .Property(reservation => reservation.Status)
                .HasConversion<string>()
                .HasMaxLength(16);
            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(reservation => reservation.Reference);
            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(reservation => new { reservation.InventoryId, reservation.Status });
            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(reservation => new { reservation.InventoryId, reservation.Reference })
                .IsUnique()
                .HasFilter("[Status] = 'Active'");
            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(reservation => reservation.ExpiresAt);
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(reservation => reservation.Inventory)
                .WithMany(inventory => inventory.Reservations)
                .HasForeignKey(reservation => reservation.InventoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(reservation => reservation.CreatedByUser)
                .WithMany(user => user.InventoryReservations)
                .HasForeignKey(reservation => reservation.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryReservation>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_InventoryReservations_Quantity",
                        "[Quantity] > 0");
                    table.HasCheckConstraint(
                        "CK_InventoryReservations_Reference",
                        "LEN(LTRIM(RTRIM([Reference]))) > 0");
                    table.HasCheckConstraint(
                        "CK_InventoryReservations_ExpiresAt",
                        "[ExpiresAt] IS NULL OR [ExpiresAt] > [CreatedAt]");
                });
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
