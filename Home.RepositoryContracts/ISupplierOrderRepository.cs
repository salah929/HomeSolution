using Home.Entities;
using Home.Shared.Enums;

namespace Home.RepositoryContracts
{
    public interface ISupplierOrderRepository
    {
        Task<SupplierOrder?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default);

        void Add(SupplierOrder order);
        void Update(SupplierOrder order);
        void Remove(SupplierOrder order);
        Task RemoveAsync(Guid orderId, CancellationToken ct = default);
        void RemoveItem(SupplierOrderItem item);
        void AddItem(SupplierOrderItem item);
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        // Search + sort + paging
        Task<IReadOnlyList<SupplierOrder>> SearchAsync(
            string? searchString = null,
            string? searchBy = null,
            Guid? supplierId = null,
            SupplierOrderStatus? status = null,
            DateTime? from = null,
            DateTime? to = null,
            string? sortBy = null,
            bool desc = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);
        Task<int> CountAsync(
            string? searchString = null,
            string? searchBy = null,
            Guid? supplierId = null,
            SupplierOrderStatus? status = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default);
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