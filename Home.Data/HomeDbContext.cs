using Home.Entities;
using Microsoft.EntityFrameworkCore;

namespace Home.Data
{
    public class HomeDbContext : DbContext
    {
        public HomeDbContext(DbContextOptions<HomeDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CustomerOrder> CustomerOrders { get; set; }
        public DbSet<CustomerOrderItem> CustomerOrderItems { get; set; }
        public DbSet<SupplierOrder> SupplierOrders { get; set; }
        public DbSet<SupplierOrderItem> SupplierOrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //// Enum conversions (stored as int by default, so the following two lines are optional)
            //modelBuilder.Entity<CustomerOrder>().Property(o => o.OrderStatus).HasConversion<int>();
            //modelBuilder.Entity<SupplierOrder>().Property(o => o.OrderStatus).HasConversion<int>();

            // the following lines are optional if the table names match the DbSet names
            // if you want to customize table names, uncomment and modify as needed
            ////modelBuilder.Entity<Product>().ToTable("Products");
            ////modelBuilder.Entity<Customer>().ToTable("Customers");
            ////modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            ////modelBuilder.Entity<CustomerOrder>().ToTable("CustomerOrders");
            ////modelBuilder.Entity<SupplierOrder>().ToTable("SupplierOrders");
            ////modelBuilder.Entity<CustomerOrderItem>().ToTable("CustomerOrderItems");
            ////modelBuilder.Entity<SupplierOrderItem>().ToTable("SupplierOrderItems");
            ///

            // Relationships

            // Customer ↔ CustomerOrder (1:N)
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Orders)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId)
                // The following line prevents deleting a customer if it has orders
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction, for SQL Server

            // CustomerOrder ↔ CustomerOrderItem (1:N)
            modelBuilder.Entity<CustomerOrder>()
                .HasMany(o => o.Items)
                .WithOne(i => i.CustomerOrder)
                .HasForeignKey(i => i.CustomerOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supplier ↔ SupplierOrder (1:N)
            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Orders)
                .WithOne(o => o.Supplier)
                .HasForeignKey(o => o.SupplierId)
                // The following line prevents deleting a supplier if it has orders
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction, for SQL Server

            // SupplierOrder ↔ SupplierOrderItem (1:N)
            modelBuilder.Entity<SupplierOrder>()
                .HasMany(o => o.Items)
                .WithOne(i => i.SupplierOrder)
                .HasForeignKey(i => i.SupplierOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product ↔ Supplier (N:1)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                // The following line is important, because if the delete behavior is not set explicitly, it will be cascade by default
                // This means, deleting a Supplier will delete all its Products and SupplierOrders, and both then cascade to SupplierOrderItems.
                // This will result two cascading paths to SupplierOrderItems, which is not allowed in SQL Server
                // Path A: Supplier → Products → SupplierOrderItems
                // Path B: Supplier → SupplierOrders → SupplierOrderItems
                // And also, deleting the products will delete all its CustomerOrderItems, which may not be what we want
                // See the comments at the end of this file for more explanation
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction, for SQL Server
                
            // Product ↔ CustomerOrderItem (1:N)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.CustomerOrderItems)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction, for SQL Server

            // Product ↔ SupplierOrderItem (1:N)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.SupplierOrderItems)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction, for SQL Server
        }
    }
}


/* Explanation of the cascading paths issue:
 * 1. Supplier relationships
 *    Supplier → Products (cascade delete by default).
 *    Supplier → SupplierOrders (cascade delete by default).
 *    So, if you delete a Supplier, EF/SQL Server will try to automatically delete both:
 *    all its Products and all its SupplierOrders.

 * 2. Deeper relationships
 *    Products → SupplierOrderItems (cascade).
 *    SupplierOrders → SupplierOrderItems (cascade).
 *    That means:
 *    If a Product is deleted, its SupplierOrderItems are deleted.
 *    If a SupplierOrder is deleted, its SupplierOrderItems are also deleted.
 *    

 * 3. The Problem: Two Cascade Paths to the Same Table
 *    Now imagine deleting a Supplier:
 *    Path A: Supplier → Products → SupplierOrderItems
 *    Path B: Supplier → SupplierOrders → SupplierOrderItems
 *    SQL Server doesn’t allow multiple cascade paths that end up deleting from the same table (SupplierOrderItems), because it creates ambiguity and risk of circular references.

 * 4. The Fix
 *    To solve this, we broke one cascade:
 *    .OnDelete(DeleteBehavior.Restrict)
 *    on the Supplier → Product relation.
 *    Now when deleting a Supplier:
 *    EF deletes SupplierOrders (and their SupplierOrderItems).
 *    EF tries to delete Products, but because delete is restricted, you must handle them manually (or set another strategy).
 *    This avoids the “multiple cascade paths” error.

 * 👉 In short: SQL Server forbids two different cascade delete chains leading to the same table
*/