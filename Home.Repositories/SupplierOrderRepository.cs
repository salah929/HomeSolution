using Home.Data;
using Home.Entities;
using Home.Shared.Enums;
using Home.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace Home.Repositories
{
    public class SupplierOrderRepository : ISupplierOrderRepository
    {
        private readonly HomeDbContext _db;
        public SupplierOrderRepository(HomeDbContext db) => _db = db;

        public async Task<SupplierOrder?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default)
        {
            // Always use IQueryable<SupplierOrder> when building a query conditionally with multiple Includes,
            // so the compiler doesn’t get confused by different IIncludableQueryable<> types.
            IQueryable<SupplierOrder> q = _db.SupplierOrders.AsNoTracking().Where(o => o.SupplierOrderId == orderId).Include(order => order.Supplier);

            if (includeItems)
                q = q.Include(order => order.Items).ThenInclude(item => item.Product);

            var order = await q.SingleOrDefaultAsync(ct);

            if (order != null && includeItems && order.Items != null)
            {
                // Sort the Items by ItemNumber in memory
                order.Items = order.Items.OrderBy(i => i.OrderItemNumber).ToList();
            }

            return order;
        }

        public async Task<IReadOnlyList<SupplierOrder>> SearchAsync(string? searchString, string? searchBy, Guid? supplierId = null,
                                                                    SupplierOrderStatus? status = null,
                                                                    DateTime? from = null, DateTime? to = null,
                                                                    string? sortBy = null, bool desc = false,
                                                                    int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            IQueryable<SupplierOrder> q = _db.SupplierOrders.AsNoTracking().Include(o => o.Supplier);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                switch (searchBy?.ToLower())
                {
                    case "ordernumber":
                        q = q.Where(o => EF.Functions.Like(o.OrderNumber, s));
                        break;
                    case "notes":
                        q = q.Where(o => o.Notes != null && EF.Functions.Like(o.Notes, s));
                        break;
                    case "supplier":
                        q = q.Where(o => EF.Functions.Like(o.Supplier!.Name, s));
                        break;
                    default:
                        q = q.Where(o =>
                            EF.Functions.Like(o.OrderNumber, s) ||
                            EF.Functions.Like(o.Notes!, s) ||
                            EF.Functions.Like(o.Supplier!.Name, s)
                        );
                        break;
                }
            }

            if (supplierId.HasValue) q = q.Where(o => o.SupplierId == supplierId.Value);
            if (status.HasValue) q = q.Where(o => o.OrderStatus == status.Value);
            if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value.Date);
            if (to.HasValue) q = q.Where(o => o.OrderDate <= to.Value.Date.AddDays(1).AddTicks(-1));

            q = ApplySorting(q, sortBy, desc);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            return await q.Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync(ct);
        }

        public Task<int> CountAsync(string? searchString, string? searchBy, Guid? supplierId = null, SupplierOrderStatus? status = null,
                                    DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            IQueryable<SupplierOrder> q = _db.SupplierOrders.AsNoTracking().Include(o => o.Supplier);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                switch (searchBy?.ToLower())
                {
                    case "ordernumber":
                        q = q.Where(o => EF.Functions.Like(o.OrderNumber, s));
                        break;
                    case "notes":
                        q = q.Where(o => o.Notes != null && EF.Functions.Like(o.Notes, s));
                        break;
                    case "supplier":
                        q = q.Where(o => EF.Functions.Like(o.Supplier!.Name, s));
                        break;
                    default:
                        q = q.Where(o =>
                            EF.Functions.Like(o.OrderNumber, s) ||
                            EF.Functions.Like(o.Notes!, s) ||
                            EF.Functions.Like(o.Supplier!.Name, s)
                        );
                        break;
                }
            }

            if (supplierId.HasValue) q = q.Where(o => o.SupplierId == supplierId.Value);
            if (status.HasValue) q = q.Where(o => o.OrderStatus == status.Value);
            if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value.Date);
            if (to.HasValue) q = q.Where(o => o.OrderDate <= to.Value.Date.AddDays(1).AddTicks(-1));

            return q.CountAsync(ct);
        }

        private static IQueryable<SupplierOrder> ApplySorting(IQueryable<SupplierOrder> q, string? sortBy, bool desc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "ordernumber" => desc ? q.OrderByDescending(o => o.OrderNumber) : q.OrderBy(o => o.OrderNumber),
                "orderdate" => desc ? q.OrderByDescending(o => o.OrderDate) : q.OrderBy(o => o.OrderDate),
                "supplier" => desc ? q.OrderByDescending(o => o.Supplier != null ?  o.Supplier!.Name : "") : q.OrderBy(o => o.Supplier!.Name),
                "notes" => desc ? q.OrderByDescending(o => o.Notes) : q.OrderBy(o => o.Notes),
                "status" => desc ? q.OrderByDescending(o => o.OrderStatus) : q.OrderBy(o => o.OrderStatus),
                _ => q.OrderByDescending(o => o.OrderDate) // default
            };
        }

        public void Add(SupplierOrder order) => _db.SupplierOrders.Add(order);
        public void Update(SupplierOrder order) => _db.SupplierOrders.Update(order);
        public void Remove(SupplierOrder order) => _db.SupplierOrders.Remove(order);

        public async Task RemoveAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.SupplierOrders.FirstOrDefaultAsync(o => o.SupplierOrderId == orderId, ct);
            if (entity != null) _db.SupplierOrders.Remove(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public void AddItem(SupplierOrderItem item) => _db.SupplierOrderItems.Add(item);
        public void RemoveItem(SupplierOrderItem item) => _db.SupplierOrderItems.Remove(item);
    }
}
