using Home.DTOs;
using Home.Entities;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Shared.Enums;

namespace Home.Services
{
    public class CustomerOrderService : ICustomerOrderService
    {
        private readonly ICustomerOrderRepository _customerOrderRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IProductRepository _productRepo;

        public CustomerOrderService(ICustomerOrderRepository orderRepo, ICustomerRepository customerRepo, IProductRepository productRepo)
        {
            _customerOrderRepo = orderRepo;
            _customerRepo = customerRepo;
            _productRepo = productRepo;
        }

        public async Task<CustomerOrderDto?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default)
        {
            var order = await _customerOrderRepo.GetByIdAsync(orderId, includeItems, ct);
            return order?.ToCustomerOrderDto(includeItems);
        }


        public async Task<IReadOnlyList<CustomerOrderDto>> SearchAsync(string? searchString, string? searchBy, Guid? customerId = null,
                                                                       CustomerOrderStatus? status = null, DateTime? from = null, DateTime? to = null,
                                                                       string? sortBy = null, bool desc = false,
                                                                       int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            IReadOnlyList<CustomerOrder> orders = await _customerOrderRepo.SearchAsync(searchString, searchBy, customerId, status, from, to, sortBy, desc, page, pageSize, ct);
            // Search includes Customer, not Items
            return orders.Select(o => o.ToCustomerOrderDto(includeItems: false)).ToList();
        }

        public Task<int> CountAsync(string? searchString, string? searchBy, Guid? customerId = null, CustomerOrderStatus? status = null,
                                    DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
            => _customerOrderRepo.CountAsync(searchString, searchBy, customerId, status, from, to, ct);

        public async Task<Guid> CreateAsync(CreateCustomerOrderDto dto, CancellationToken ct = default)
        {
            // Validate dto
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var errors = new List<(string Key, string Message)>();

            // Validate order number
            if (string.IsNullOrWhiteSpace(dto.OrderNumber)) errors.Add((nameof(dto.OrderNumber), "Order number is required."));

            // Ensure dto.Items is not null to simplify later logic
            if (dto.Items is null) dto.Items = new List<CreateCustomerOrderItemDto>(); // or dto.Items ??= new List<CreateCustomerOrderItemDto>();

            // Validate customer
            Customer? customer = await _customerRepo.GetByIdAsync(dto.CustomerId, false, ct);
            if (customer is null) errors.Add((nameof(dto.CustomerId), "Customer is required."));

            // get distinct productIds from dto.Items to minimize db calls
            var productIds = dto.Items.Select(i => i.ProductId).Distinct();
            var products = new List<Product>();
            // Right now, you do a loop with await inside.
            // This is fine, but could be parallelized or replaced with GetByIdsAsync for one query. Example (optional):
            // var products = await Task.WhenAll(productIds.Select(id => _productRepo.GetByIdAsync(id, ct)));
            // var productsDict = products.Where(p => p != null).ToDictionary(p => p.ProductId);
            foreach (var id in productIds)
            {
                var product = await _productRepo.GetByIdAsync(id, ct);
                if (product != null) products.Add(product);
            }
            var productsDict = products.ToDictionary(p => p.ProductId);

            // Validate products and quantities
            foreach (var item in dto.Items)
            {
                if (!productsDict.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add((nameof(item.ProductId), $"Product not found: {item.ProductId}"));
                    continue;
                }
                if (item.Quantity < 1) errors.Add((nameof(item.Quantity), $"Quantity for product {product.Name} must be at least 1."));
                if (item.UnitPrice <= 0) errors.Add((nameof(item.UnitPrice), $"Unit price for product {product.Name} must be greater than 0."));
            }

            // Validate duplicate OrderItemIds in dto.Items
            var duplicateIds = dto.Items.GroupBy(i => i.ProductId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateIds.Any())
            {
                var duplicateNames = duplicateIds.Select(id => productsDict[id].Name);
                errors.Add((nameof(CreateCustomerOrderItemDto.ProductId), $"Duplicate products: {string.Join(", ", duplicateNames)}"));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            CustomerOrder order = dto.ToCustomerOrder();

            _customerOrderRepo.Add(order);
            await _customerOrderRepo.SaveChangesAsync(ct);
            return order.CustomerOrderId;
        }

        public async Task<bool> UpdateAsync(UpdateCustomerOrderDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var errors = new List<(string Key, string Message)>();

            // Fetch existing order including items
            CustomerOrder? existingOrder = await _customerOrderRepo.GetByIdAsync(dto.OrderId, includeItems: true, ct);
            if (existingOrder == null)
            {
                errors.Add((string.Empty, "Order not found."));
                throw new DomainValidationException(errors);
            }

            // Validate order number
            if (string.IsNullOrWhiteSpace(dto.OrderNumber)) errors.Add((nameof(dto.OrderNumber), "Order number is required."));

            // Ensure dto.Items is not null to simplify later logic
            dto.Items ??= new(); // or dto.Items ??= [];

            // Validate customer
            Customer? customer = await _customerRepo.GetByIdAsync(dto.CustomerId, false, ct);
            if (customer == null)
                errors.Add((nameof(dto.CustomerId), "Customer is required."));

            // Fetch products
            var productIds = dto.Items.Select(i => i.ProductId).Distinct();
            var products = new List<Product>();
            foreach (var id in productIds)
            {
                var product = await _productRepo.GetByIdAsync(id, ct);
                if (product != null) products.Add(product);
            }
            var productsDict = products.ToDictionary(p => p.ProductId);

            // Validate products and quantities
            foreach (var item in dto.Items)
            {
                if (!productsDict.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add((nameof(item.ProductId), $"Product not found: {item.ProductId}"));
                    continue;
                }
                if (item.Quantity < 1) errors.Add((nameof(item.Quantity), $"Quantity for product {product.Name} must be at least 1."));
                if (item.UnitPrice <= 0) errors.Add((nameof(item.UnitPrice), $"Unit price for product {product.Name} must be greater than 0."));
            }

            // Check for duplicate products in dto.Items
            var duplicateIds = dto.Items.GroupBy(i => i.ProductId)
                                        .Where(g => g.Count() > 1)
                                        .Select(g => g.Key)
                                        .ToList();
            if (duplicateIds.Any())
            {
                var duplicateNames = duplicateIds.Select(id => productsDict[id].Name);
                errors.Add((nameof(UpdateCustomerOrderItemDto.ProductId), $"Duplicate products: {string.Join(", ", duplicateNames)}"));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            existingOrder.OrderNumber = dto.OrderNumber.Trim();
            existingOrder.Customer = null; // important to avoid EF Core tracking issues
            existingOrder.CustomerId = dto.CustomerId;
            existingOrder.OrderDate = dto.OrderDate;
            existingOrder.Notes = dto.Notes?.Trim();
            existingOrder.OrderStatus = dto.OrderStatus;

            // Sync items

            // the following line creates a dictionary of the existing CustomerOrderItems, which stored in the db,
            // key is CustomerOrderItemId, value is the CustomerOrderItem itself
            var existingItems = existingOrder.Items.ToDictionary(x => x.OrderItemId);

            // the following line creates a set of the incoming item IDs from the DTO
            // this helps to quickly check which items are still present in the updateDTO
            // we use a HashSet for O(1) lookup times
            // while ToList or array would require O(n) lookups
            // details at the end of this file
            var incomingIds = dto.Items.Select(x => x.ItemId).ToHashSet();

            // Update or add
            foreach (var item in dto.Items)
            {
                if (existingItems.TryGetValue(item.ItemId, out CustomerOrderItem? ei))
                {
                    // Update existing
                    ei.OrderItemNumber = item.ItemNumber;
                    ei.Product = null;
                    ei.ProductId = item.ProductId;
                    ei.Quantity = item.Quantity;
                    ei.UnitPrice = item.UnitPrice;

                    // Remove from dictionary to track items still present (for deletion later)
                    existingItems.Remove(item.ItemId);
                }
                else
                {
                    // Add new item
                    _customerOrderRepo.AddItem(new CustomerOrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        CustomerOrderId = existingOrder.CustomerOrderId,
                        OrderItemNumber = item.ItemNumber,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            // Remove items that are no longer present in DTO
            foreach (var obsoleteItem in existingItems.Values)
            {
                _customerOrderRepo.RemoveItem(obsoleteItem);
            }

            _customerOrderRepo.Update(existingOrder);
            await _customerOrderRepo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid orderId, CancellationToken ct = default)
        {
            CustomerOrder? existingCustomerOrder = await _customerOrderRepo.GetByIdAsync(orderId, includeItems: false, ct);
            if (existingCustomerOrder is null) return false;

            _customerOrderRepo.Remove(existingCustomerOrder);
            await _customerOrderRepo.SaveChangesAsync(ct);
            return true;
        }
    }
}

/* Why ToHashSet()? - That choice is about performance.
 * You later check:
 * if (!incomingIds.Contains(kv.Key)) { ... }
 * If incomingIds is a list, .Contains(...) is O(n) (scans the list every time).
 * If incomingIds is a hash set, .Contains(...) is O(1) (constant-time lookup).
 * 
 * 👉 So if you have 100 existing items and 100 incoming items:
 *    With List: worst case 100 × 100 = 10,000 comparisons.
 *    With HashSet: 100 lookups, done.
 *    
 * ⚖️ Summary:
 *    ToList() would work, but slower for lookups.
 *    ToHashSet() is chosen because the main operation here is fast membership check (Contains).
 */