using Home.Data;
using Home.Entities;
using Home.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Home.Web;
public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HomeDbContext>();

        if (await db.Suppliers.AnyAsync()) return; // already seeded

        var random = new Random();

        // ===== Suppliers (25) =====
        var suppliers = Enumerable.Range(1, 25)
            .Select(i => new Supplier
            {
                SupplierId = Guid.NewGuid(),
                Name = $"Supplier {i}",
                Email = $"supplier{i}@example.com",
                PhoneNumber = $"+1-555-01{i:00}000",
                Address = $"{100 + i} Supplier St, City {i}",
                ContactPerson = $"Supplier Contact {i}"
            }).ToList();
        db.Suppliers.AddRange(suppliers);

        // ===== Customers (25) =====
        var customers = Enumerable.Range(1, 25)
            .Select(i => new Customer
            {
                CustomerId = Guid.NewGuid(),
                Name = $"Customer {i}",
                Email = $"customer{i}@example.com",
                PhoneNumber = $"+1-555-02{i:00}000",
                Address = $"{200 + i} Customer Ave, City {i}",
                ContactPerson = $"Customer Contact {i}"
            }).ToList();
        db.Customers.AddRange(customers);

        // ===== Products (100) =====
        var products = Enumerable.Range(1, 100)
            .Select(i =>
            {
                var sup = suppliers[random.Next(suppliers.Count)];
                return new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = $"P-{1000 + i}",
                    Name = $"Product {i}",
                    SupplierId = sup.SupplierId,
                    Description = $"Sample description for Product {i}."
                };
            }).ToList();
        db.Products.AddRange(products);

        // ===== Supplier Orders (25) =====
        var supplierOrders = Enumerable.Range(1, 25)
            .Select(i =>
            {
                var sup = suppliers[random.Next(suppliers.Count)];
                var supplierProducts = products.Where(p => p.SupplierId == sup.SupplierId).ToList();

                // Skip if supplier has no products
                if (!supplierProducts.Any()) return null;

                var itemsCount = random.Next(5, 11); // 5–10 items
                var items = Enumerable.Range(1, itemsCount)
                    .Select(_ =>
                    {
                        var prod = supplierProducts[random.Next(supplierProducts.Count)];
                        return new SupplierOrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            ProductId = prod.ProductId,
                            Quantity = random.Next(50, 500)
                        };
                    }).ToList();

                return new SupplierOrder
                {
                    SupplierOrderId = Guid.NewGuid(),
                    SupplierId = sup.SupplierId,
                    OrderNumber = $"SO-{1000 + i}",
                    OrderDate = DateTime.UtcNow.AddDays(-random.Next(0, 100)),
                    OrderStatus = SupplierOrderStatus.Confirmed,
                    Notes = $"Notes for supplier order {i}.",
                    Items = items
                };
            })
            .Where(o => o != null) // filter out nulls
            .ToList();
        if (supplierOrders != null) db.SupplierOrders.AddRange(supplierOrders!);

        // ===== Customer Orders (25) =====
        var customerOrders = Enumerable.Range(1, 25)
            .Select(i =>
            {
                var cust = customers[random.Next(customers.Count)];
                var itemsCount = random.Next(5, 11); // 5–10 items
                var items = Enumerable.Range(1, itemsCount)
                    .Select(_ =>
                    {
                        var prod = products[random.Next(products.Count)];
                        return new CustomerOrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            ProductId = prod.ProductId,
                            Quantity = random.Next(1, 50)
                        };
                    }).ToList();

                return new CustomerOrder
                {
                    CustomerOrderId = Guid.NewGuid(),
                    CustomerId = cust.CustomerId,
                    OrderNumber = $"CO-{1000 + i}",
                    OrderDate = DateTime.UtcNow.AddDays(-random.Next(0, 100)),
                    OrderStatus = CustomerOrderStatus.Confirmed,
                    Notes = $"Notes for customer order {i}.",
                    Items = items
                };
            }).ToList();
        db.CustomerOrders.AddRange(customerOrders);

        await db.SaveChangesAsync();
    }

}
