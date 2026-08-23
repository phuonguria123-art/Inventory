using Inventory.Domain.Entities;
using Inventory.Domain.Entity;
using Inventory.Domain.Entitys;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Inventories> Inventories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchanseOrder> PurchanseOrders { get; set; }
        public DbSet<PurchanseOrderDetail> PurchanseOrdersDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
