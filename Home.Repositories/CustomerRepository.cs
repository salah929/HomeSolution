using Home.Data;
using Home.Entities;
using Home.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace Home.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly HomeDbContext _db;
        public CustomerRepository(HomeDbContext db) => _db = db;

        public async Task<Customer?> GetByIdAsync(Guid customerId, bool includeOrders = false, CancellationToken ct = default)
        {
            var q = _db.Customers.AsNoTracking().Where(c => c.CustomerId == customerId);
            if (includeOrders) q = q.Include(c => c.Orders);
            return await q.FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Customers.AsNoTracking().ToListAsync(ct);

        public async Task<IReadOnlyList<CustomerOrder>> GetOrdersByCustomerAsync(Guid customerId, bool includeItems = false,
                                                                                 CancellationToken ct = default)
        {
            var q = _db.CustomerOrders.AsNoTracking().Where(o => o.CustomerId == customerId);
            if (includeItems) q = q.Include(o => o.Items).ThenInclude(i => i.Product);
            return await q.OrderByDescending(o => o.OrderDate).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Customer>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                               string? sortBy = null, bool desc = false, int page = 1, int pageSize = 20,
                                                               CancellationToken ct = default)
        {
            IQueryable<Customer> q = _db.Customers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "name" => q.Where(c => EF.Functions.Like(c.Name, s)),
                    "email" => q.Where(c => EF.Functions.Like(c.Email!, s)),
                    "phonenumber" => q.Where(c => EF.Functions.Like(c.PhoneNumber!, s)),
                    "address" => q.Where(c => EF.Functions.Like(c.Address!, s)),
                    "contactperson" => q.Where(c => EF.Functions.Like(c.ContactPerson!, s)),
                    _ => q.Where(c =>
                         EF.Functions.Like(c.Name, s) ||
                         EF.Functions.Like(c.Email!, s) ||
                         EF.Functions.Like(c.PhoneNumber!, s) ||
                         EF.Functions.Like(c.Address!, s) ||
                         EF.Functions.Like(c.ContactPerson!, s))
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
            IQueryable<Customer> q = _db.Customers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                q = (searchBy?.ToLowerInvariant()) switch
                {
                    "name" => q.Where(c => EF.Functions.Like(c.Name, s)),
                    "email" => q.Where(c => EF.Functions.Like(c.Email!, s)),
                    "phonenumber" => q.Where(c => EF.Functions.Like(c.PhoneNumber!, s)),
                    "address" => q.Where(c => EF.Functions.Like(c.Address!, s)),
                    "contactperson" => q.Where(c => EF.Functions.Like(c.ContactPerson!, s)),
                    _ => q.Where(c =>
                         EF.Functions.Like(c.Name, s) ||
                         EF.Functions.Like(c.Email!, s) ||
                         EF.Functions.Like(c.PhoneNumber!, s) ||
                         EF.Functions.Like(c.Address!, s) ||
                         EF.Functions.Like(c.ContactPerson!, s))
                };
            }

            return q.CountAsync(ct);
        }

        private static IQueryable<Customer> ApplySorting(IQueryable<Customer> q, string? sortBy, bool desc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "name" => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
                "email" => desc ? q.OrderByDescending(c => c.Email) : q.OrderBy(c => c.Email),
                "phonenumber" => desc ? q.OrderByDescending(c => c.PhoneNumber) : q.OrderBy(c => c.PhoneNumber),
                "address" => desc ? q.OrderByDescending(c => c.Address) : q.OrderBy(c => c.Address),
                "contactperson" => desc ? q.OrderByDescending(c => c.ContactPerson) : q.OrderBy(c => c.ContactPerson),
                _ => q.OrderBy(c => c.Name) // default
            };
        }

        public void Add(Customer customer) => _db.Customers.Add(customer);
        public void Update(Customer customer) => _db.Customers.Update(customer);
        public void Remove(Customer customer) => _db.Customers.Remove(customer);

        public async Task Remove(Guid customerId, CancellationToken ct = default)
        {
            var entity = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
            if (entity != null) _db.Customers.Remove(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
