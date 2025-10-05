using Home.Data;
using Home.Entities;
using Home.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace Home.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly HomeDbContext _db;
        public SupplierRepository(HomeDbContext db) => _db = db;

        public async Task<Supplier?> GetByIdAsync(Guid supplierId, bool includeOrders = false, bool includeProducts = false, CancellationToken ct = default)
        {
            var q = _db.Suppliers.AsNoTracking().Where(s => s.SupplierId == supplierId);
            if (includeOrders) q = q.Include(s => s.Orders);
            if (includeProducts) q = q.Include(s => s.Products);
            return await q.FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Suppliers.AsNoTracking().ToListAsync(ct);

        public async Task<IReadOnlyList<SupplierOrder>> GetOrdersBySupplierAsync(Guid supplierId, bool includeItems = false,
                                                                                 CancellationToken ct = default)
        {
            var q = _db.SupplierOrders.AsNoTracking().Where(o => o.SupplierId == supplierId);

            if (includeItems) q = q.Include(o => o.Items).ThenInclude(i => i.Product);

            return await q.OrderByDescending(o => o.OrderDate).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Product>> GetProductsBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        {
            return await _db.Products.AsNoTracking().Where(p => p.SupplierId == supplierId).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Supplier>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                               string? sortBy = null, bool desc = false, int page = 1, int pageSize = 20,
                                                               CancellationToken ct = default)
        {
            IQueryable<Supplier> q = _db.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "name" => q.Where(sup => EF.Functions.Like(sup.Name, s)),
                    "email" => q.Where(sup => EF.Functions.Like(sup.Email!, s)),
                    "phonenumber" => q.Where(sup => EF.Functions.Like(sup.PhoneNumber!, s)),
                    "address" => q.Where(sup => EF.Functions.Like(sup.Address!, s)),
                    "contactperson" => q.Where(sup => EF.Functions.Like(sup.ContactPerson!, s)),
                    _ => q.Where(sup =>
                         EF.Functions.Like(sup.Name, s) ||
                         EF.Functions.Like(sup.Email!, s) ||
                         EF.Functions.Like(sup.PhoneNumber!, s) ||
                         EF.Functions.Like(sup.Address!, s) ||
                         EF.Functions.Like(sup.ContactPerson!, s))
                };
            }

            q = ApplySorting(q, sortBy, desc);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            return await q.Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task<int> CountAsync(string? searchString = null, string? searchBy = null, CancellationToken ct = default)
        {
            IQueryable<Supplier> q = _db.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "name" => q.Where(sup => EF.Functions.Like(sup.Name, s)),
                    "email" => q.Where(sup => EF.Functions.Like(sup.Email!, s)),
                    "phonenumber" => q.Where(sup => EF.Functions.Like(sup.PhoneNumber!, s)),
                    "address" => q.Where(sup => EF.Functions.Like(sup.Address!, s)),
                    "contactperson" => q.Where(sup => EF.Functions.Like(sup.ContactPerson!, s)),
                    _ => q.Where(sup =>
                         EF.Functions.Like(sup.Name, s) ||
                         EF.Functions.Like(sup.Email!, s) ||
                         EF.Functions.Like(sup.PhoneNumber!, s) ||
                         EF.Functions.Like(sup.Address!, s) ||
                         EF.Functions.Like(sup.ContactPerson!, s))
                };
            }

            return q.CountAsync(ct);
        }

        private static IQueryable<Supplier> ApplySorting(IQueryable<Supplier> q, string? sortBy, bool desc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "name" => desc ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
                "email" => desc ? q.OrderByDescending(s => s.Email) : q.OrderBy(s => s.Email),
                "phonenumber" => desc ? q.OrderByDescending(s => s.PhoneNumber) : q.OrderBy(s => s.PhoneNumber),
                "address" => desc ? q.OrderByDescending(s => s.Address) : q.OrderBy(s => s.Address),
                "contactperson" => desc ? q.OrderByDescending(s => s.ContactPerson) : q.OrderBy(s => s.ContactPerson),
                _ => q.OrderBy(s => s.Name) // default
            };
        }

        public void Add(Supplier supplier) => _db.Suppliers.Add(supplier);
        public void Update(Supplier supplier) => _db.Suppliers.Update(supplier);
        public void Remove(Supplier supplier) => _db.Suppliers.Remove(supplier);

        public async Task Remove(Guid supplierId, CancellationToken ct = default)
        {
            var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId, ct);
            if (entity != null) _db.Suppliers.Remove(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
