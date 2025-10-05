using Home.Data;
using Home.Entities;
using Home.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace Home.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly HomeDbContext _db;
        public ProductRepository(HomeDbContext db) => _db = db;

        public Task<bool> CodeExistsAsync(string code, Guid? excludeProductId = null, CancellationToken ct = default) =>
            // CodeExistsAsync is meant to enforce uniqueness of the product code.
            // When adding a new product → it checks if that code already exists.
            // When updating a product → it checks if another product (different ID) already has that code.
            // 👉 This prevents two different products from ending up with the same code.
            _db.Products.AsNoTracking().AnyAsync(p => p.Code == code && (excludeProductId == null || p.ProductId != excludeProductId), ct);

        public Task<int> CountAsync(string? searchString = null, string? searchBy = null, Guid? supplierId = null,
                                    CancellationToken ct = default)
        {
            IQueryable<Product> q = _db.Products.AsNoTracking();

            if (supplierId.HasValue)
                q = q.Where(p => p.SupplierId == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "code" => q.Where(p => EF.Functions.Like(p.Code, s)),
                    "name" => q.Where(p => EF.Functions.Like(p.Name!, s)),
                    "description" => q.Where(p => EF.Functions.Like(p.Description!, s)),
                    _ => q.Where(p =>
                             EF.Functions.Like(p.Code, s) ||
                             EF.Functions.Like(p.Name!, s) ||
                             EF.Functions.Like(p.Description!, s))
                };
            }

            return q.CountAsync(ct);
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Products
                .AsNoTracking()
                // .OrderBy(p => p.Code) // optional: add default ordering
                .ToListAsync(ct);

        public async Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Code == code, ct);
        }
        // Alternative expression-bodied version:
        // Here, you return the task directly from FirstOrDefaultAsync(...).
        // No await is used → so you don’t need the async keyword. details in the end of this file.
        //public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        //    _db.Products
        //        .AsNoTracking()
        //        .Include(p => p.Supplier)
        //        .FirstOrDefaultAsync(p => p.Code == code, ct);

        public async Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default)
        {
            return await _db.Products
                .AsNoTracking() // improves performance for read-only queries by disabling change tracking, details in the end of this file
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        }
        
        public async Task<IReadOnlyList<Product>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        {
            return await _db.Products
                .AsNoTracking()
                // .Include(p => p.Supplier) // optional: include Supplier details if needed
                .Where(p => p.SupplierId == supplierId)
                // .OrderBy(p => p.Code) // optional: add default ordering
                .ToListAsync(ct);
        }

        public void Add(Product product) => _db.Products.Add(product);

        public void Remove(Product product) => _db.Products.Remove(product);

        public async Task Remove(Guid productId, CancellationToken ct = default)
        {
            var entity = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
            if (entity != null) _db.Products.Remove(entity);
        }

        public void Update(Product product) => _db.Products.Update(product);

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public async Task<IReadOnlyList<Product>> SearchAsync(string? searchString = null, string? searchBy = null, Guid? supplierId = null,
                                                              string? sortBy = null, bool desc = false, int page = 1, int pageSize = 20,
                                                              CancellationToken ct = default)
        {
            IQueryable<Product> q = _db.Products.AsNoTracking().Include(p => p.Supplier);

            if (supplierId.HasValue)
                q = q.Where(p => p.SupplierId == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "code" => q.Where(p => EF.Functions.Like(p.Code, s)),
                    "name" => q.Where(p => EF.Functions.Like(p.Name!, s)),
                    "description" => q.Where(p => EF.Functions.Like(p.Description!, s)),
                    _ => q.Where(p =>
                             EF.Functions.Like(p.Code, s) ||
                             EF.Functions.Like(p.Name!, s) ||
                             EF.Functions.Like(p.Description!, s))
                };
            }

            q = ApplySorting(q, sortBy, desc);

            if (page < 1) page = 1; // Prevents invalid value (like page = 0)
            if (pageSize < 1) pageSize = 20; // Prevents invalid value (like pageSize = 0)

            var items = await q.Skip((page - 1) * pageSize) // Skip the previous pages records
                               .Take(pageSize) // Take only the number of records for the current page
                               .ToListAsync(ct); // Execute the query and get the results
            return items;
        }

        public async Task<List<CustomerOrder>> GetCustomerOrdersByProductId(Guid productId, CancellationToken ct)
        {
            return await _db.CustomerOrders
                .AsNoTracking()
                .Where(o => o.Items.Any(i => i.ProductId == productId))
                .ToListAsync(ct);
        }

        public async Task<List<SupplierOrder>> GetSupplierOrdersByProductId(Guid productId, CancellationToken ct)
        {
            return await _db.SupplierOrders
                .AsNoTracking()
                .Where(o => o.Items.Any(i => i.ProductId == productId))
                .ToListAsync(ct);
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> q, string? sortBy, bool desc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "code" => desc ? q.OrderByDescending(p => p.Code) : q.OrderBy(p => p.Code),
                "name" => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
                "description" => desc ? q.OrderByDescending(p => p.Description) : q.OrderBy(p => p.Description),
                "supplier" => desc ? q.OrderByDescending(p => p.Supplier!.Name) : q.OrderBy(p => p.Supplier!.Name),
                _ => q.OrderBy(p => p.Code) // default
            };
        }
    }
}

/* AsNoTracking()
 * Improves performance for read-only queries by disabling change tracking.
 * Use it when you don’t need to update the entities returned by the query.
 * It reduces memory usage and speeds up query execution.
 * 
 * Example:
 * var products = await _db.Products.AsNoTracking().ToListAsync();
 * 
 * Note:
 * If you need to update the entities later, don’t use AsNoTracking() on that query.
 * You can always re-attach the entity to the DbContext later if needed.
 */


/* In the expression-bodied version you had:
 * 
 * public Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default) =>
      _db.Products
          .AsNoTracking()
          .Include(p => p.Supplier)
          .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
 
 * Here, you return the task directly from FirstOrDefaultAsync(...).
 * No await is used → so you don’t need the async keyword.

 * When you expand it to a block body:

 * public async Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default)
 * {
 *     return await _db.Products
 *         .AsNoTracking()
 *         .Include(p => p.Supplier)
 *         .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
 * }

 * Now you’re using await inside → so you must add async in the method signature.

 * 👉 Rule of thumb:
 * If you just return the task → no async.
 * If you await inside → must use async.
 */