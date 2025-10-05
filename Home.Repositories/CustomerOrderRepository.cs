using Home.Data;
using Home.Entities;
using Home.Shared.Enums;
using Home.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace Home.Repositories
{
    public class CustomerOrderRepository : ICustomerOrderRepository
    {
        private readonly HomeDbContext _db;
        public CustomerOrderRepository(HomeDbContext db) => _db = db;

        public async Task<CustomerOrder?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default)
        {
            // Always use IQueryable<CustomerOrder> (not var) when building a query conditionally with multiple Includes,
            // so the compiler doesn’t get confused by different IIncludableQueryable<> types.
            IQueryable<CustomerOrder> q = _db.CustomerOrders.AsNoTracking().Where(o => o.CustomerOrderId == orderId).Include(order => order.Customer);
            if (q.Count() == 0) return null;
            if (includeItems)
                q = q.Include(o => o.Items).ThenInclude(i => i.Product);

            var order = await q.SingleOrDefaultAsync(ct);

            if (order != null && includeItems && order.Items != null)
            {
                // Sort the Items by ItemNumber in memory
                order.Items = order.Items.OrderBy(i => i.OrderItemNumber).ToList();
            }

            return order;
        }

        public async Task<IReadOnlyList<CustomerOrder>> SearchAsync(string? searchString, string? searchBy, Guid? customerId = null,
                                                                    CustomerOrderStatus? status = null, DateTime? from = null, DateTime? to = null,
                                                                    string? sortBy = null, bool desc = false,
                                                                    int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            IQueryable<CustomerOrder> q = _db.CustomerOrders.AsNoTracking().Include(o => o.Customer);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                switch (searchBy?.ToLower())
                {
                    case "ordernumber":
                        q = q.Where(o => EF.Functions.Like(o.OrderNumber, s));
                        //q = q.Where(o => o.OrderNumber.Contains(searchString));
                        break;
                    case "notes":
                        q = q.Where(o => o.Notes != null && EF.Functions.Like(o.Notes, s));
                        //q = q.Where(o => o.Notes != null && o.Notes.Contains(searchString));
                        break;
                    case "customer":
                        q= q.Where(o => EF.Functions.Like(o.Customer!.Name, s));
                        //q = q.Where(o => o.Customer != null && o.Customer.Name.Contains(searchString));
                        break;
                    default:
                        q = q.Where(o =>
                            EF.Functions.Like(o.OrderNumber, s) ||
                            EF.Functions.Like(o.Notes!, s) ||
                            EF.Functions.Like(o.Customer!.Name, s)
                            //o.OrderNumber.Contains(searchString) ||
                            //(o.Notes != null && o.Notes.Contains(searchString)) ||
                            //(o.Customer != null && o.Customer.Name.Contains(searchString))
                        );
                        break;
                }
            }

            if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId.Value);
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


        public Task<int> CountAsync(string? searchString, string? searchBy, Guid? customerId = null, CustomerOrderStatus? status = null,
                                    DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            IQueryable<CustomerOrder> q = _db.CustomerOrders.AsNoTracking().Include(o => o.Customer);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = $"%{searchString.Trim()}%";
                switch (searchBy?.ToLower())
                {
                    case "ordernumber":
                        q = q.Where(o => EF.Functions.Like(o.OrderNumber, s));
                        //q = q.Where(o => o.OrderNumber.Contains(searchString));
                        break;
                    case "notes":
                        q = q.Where(o => o.Notes != null && EF.Functions.Like(o.Notes, s));
                        //q = q.Where(o => o.Notes != null && o.Notes.Contains(searchString));
                        break;
                    case "customer":
                        q = q.Where(o => EF.Functions.Like(o.Customer!.Name, s));
                        //q = q.Where(o => o.Customer != null && o.Customer.Name.Contains(searchString));
                        break;
                    default:
                        q = q.Where(o =>
                            EF.Functions.Like(o.OrderNumber, s) ||
                            EF.Functions.Like(o.Notes!, s) ||
                            EF.Functions.Like(o.Customer!.Name, s)
                        //o.OrderNumber.Contains(searchString) ||
                        //(o.Notes != null && o.Notes.Contains(searchString)) ||
                        //(o.Customer != null && o.Customer.Name.Contains(searchString))
                        );
                        break;
                }
            }

            if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId.Value);
            if (status.HasValue) q = q.Where(o => o.OrderStatus == status.Value);
            if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value.Date);
            if (to.HasValue) q = q.Where(o => o.OrderDate <= to.Value.Date.AddDays(1).AddTicks(-1));

            return q.CountAsync(ct);
        }

        private static IQueryable<CustomerOrder> ApplySorting(IQueryable<CustomerOrder> q, string? sortBy, bool desc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "ordernumber" => desc ? q.OrderByDescending(o => o.OrderNumber) : q.OrderBy(o => o.OrderNumber),
                "orderdate" => desc ? q.OrderByDescending(o => o.OrderDate) : q.OrderBy(o => o.OrderDate),
                "customer" => desc ? q.OrderByDescending(o => o.Customer != null ? o.Customer.Name : "") : q.OrderBy(o => o.Customer!.Name),
                "notes" => desc ? q.OrderByDescending(o => o.Notes) : q.OrderBy(o => o.Notes),
                "status" => desc ? q.OrderByDescending(o => o.OrderStatus) : q.OrderBy(o => o.OrderStatus),
                _ => q.OrderByDescending(o => o.OrderDate) // default
            };
        }

        public void Add(CustomerOrder order) => _db.CustomerOrders.Add(order);
        public void Update(CustomerOrder order) => _db.CustomerOrders.Update(order);
        public void Remove(CustomerOrder order) => _db.CustomerOrders.Remove(order);

        public async Task RemoveAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.CustomerOrders.FirstOrDefaultAsync(o => o.CustomerOrderId == orderId, ct);
            if (entity != null) _db.CustomerOrders.Remove(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public void AddItem(CustomerOrderItem item) => _db.CustomerOrderItems.Add(item);
        public void RemoveItem(CustomerOrderItem item) => _db.CustomerOrderItems.Remove(item);
    }
}
