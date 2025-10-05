using Home.Entities;

namespace Home.RepositoryContracts
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default);
        Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid? excludeProductId = null, CancellationToken ct = default);
        Task<IReadOnlyList<Product>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default);
        Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);

        void Add(Product product);
        void Update(Product product);
        void Remove(Product product);
        Task Remove(Guid productId, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        // Search + sort + paging
        Task<IReadOnlyList<Product>> SearchAsync(
            string? searchString = null,
            string? searchBy = null,
            Guid? supplierId = null,
            string? sortBy = null,
            bool desc = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<int> CountAsync(
            string? searchString = null,
            string? searchBy = null,
            Guid? supplierId = null,
            CancellationToken ct = default);

        Task<List<CustomerOrder>> GetCustomerOrdersByProductId(Guid productId, CancellationToken ct = default);
        Task<List<SupplierOrder>> GetSupplierOrdersByProductId(Guid productId, CancellationToken ct = default);
    }
}

/* CancellationToken ct = default 
 * is a parameter that allows the caller to cancel a running async operation (like a database query).
 * 
 * CancellationToken comes from System.Threading.
 * When you pass it into EF Core async methods (e.g., ToListAsync(ct)), EF can stop if the caller requests cancellation.
 * = default just means it’s optional — if the caller doesn’t provide a token, EF uses CancellationToken.None.

 * 👉 In practice:
 * Useful in web APIs where a client may disconnect.
 * Safe to include in repository methods — it follows modern async best practices.

 * Update and Remove in EF Core are synchronous, in-memory operations — they just change the entity’s state in the DbContext (e.g., mark it as modified or deleted).
 * No database call happens until you call SaveChangesAsync(ct).
 * That’s why you don’t need CancellationToken on Update/Remove, but you do need it on async database calls like GetAllAsync, GetByIdAsync, or SaveChangesAsync.
*/

/* keep SaveChangesAsync separate.
 * Add/Update/Remove just change tracking state; no DB call yet.
 * Calling SaveChanges inside them breaks unit-of-work, prevents batching multiple ops in one transaction, and hurts performance.
 * You can auto‑save in those methods, but you’ll lose transactional control. Best practice: services call SaveChangesAsync once per unit of work.
*/