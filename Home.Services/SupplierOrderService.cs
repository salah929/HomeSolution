using Home.DTOs;
using Home.Entities;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Shared.Enums;

namespace Home.Services
{
    public class SupplierOrderService : ISupplierOrderService
    {
        private readonly ISupplierOrderRepository _supplierOrderRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IProductRepository _productRepo;

        public SupplierOrderService(ISupplierOrderRepository supplierOrderRepo, ISupplierRepository supplierRepo, IProductRepository productRepo)
        {
            _supplierOrderRepo = supplierOrderRepo;
            _supplierRepo = supplierRepo;
            _productRepo = productRepo;
        }

        public async Task<SupplierOrderDto?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default)
        {
            var order = await _supplierOrderRepo.GetByIdAsync(orderId, includeItems, ct);
            return order?.ToSupplierOrderDto(includeItems);
        }

        public async Task<IReadOnlyList<SupplierOrderDto>> SearchAsync(string? searchString, string? searchBy, Guid? supplierId = null,
                                                           SupplierOrderStatus? status = null, DateTime? from = null, DateTime? to = null,
                                                           string? sortBy = null, bool desc = false,
                                                           int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            IReadOnlyList<SupplierOrder> orders = await _supplierOrderRepo.SearchAsync(searchString, searchBy, supplierId, status, from, to, sortBy, desc, page, pageSize, ct);
            // Search includes Supplier, not Items
            return orders.Select(o => o.ToSupplierOrderDto(includeItems: false)).ToList();
        }

        public Task<int> CountAsync(string? searchString, string? searchBy, Guid? supplierId = null, SupplierOrderStatus? status = null,
                                    DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
            => _supplierOrderRepo.CountAsync(searchString, searchBy, supplierId, status, from, to, ct);


        public async Task<Guid> CreateAsync(CreateSupplierOrderDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var errors = new List<(string Key, string Message)>();

            if (string.IsNullOrWhiteSpace(dto.OrderNumber))
                errors.Add((nameof(dto.OrderNumber), "Order number is required."));

            dto.Items ??= new List<CreateSupplierOrderItemDto>();

            Supplier? supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, false, false, ct);
            if (supplier is null)
                errors.Add((nameof(dto.SupplierId), "Supplier is required."));

            // Get distinct productIds
            var productIds = dto.Items.Select(i => i.ProductId).Distinct();
            var products = new List<Product>();

            foreach (var id in productIds)
            {
                var product = await _productRepo.GetByIdAsync(id, ct);
                if (product != null) products.Add(product);
            }

            var productsDict = products.ToDictionary(p => p.ProductId);

            for (var i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                if (!productsDict.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add((nameof(item.ProductId), $"Product not found: {item.ProductId}"));
                    continue;
                }
                if (product.SupplierId != dto.SupplierId)
                {
                    //errors.Add(($"OrderItems[{i}].ProductId", $"Product {product.Name} does not belong to the selected supplier."));
                    errors.Add((string.Empty, $"Product {product.Name} does not belong to the selected supplier."));
                    continue;
                }
                if (item.Quantity < 1) errors.Add((nameof(item.Quantity), $"Quantity for product {product.Name} must be at least 1."));
                if (item.UnitPrice <= 0) errors.Add((nameof(item.UnitPrice), $"Unit price for product {product.Name} must be greater than 0."));
            }

            var duplicateIds = dto.Items.GroupBy(i => i.ProductId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateIds.Any())
            {
                var duplicateNames = duplicateIds.Select(id => productsDict[id].Name);
                //errors.Add((nameof(CreateSupplierOrderItemDto.ProductId), $"Duplicate products: {string.Join(", ", duplicateNames)}"));
                errors.Add((string.Empty, $"Duplicate products: {string.Join(", ", duplicateNames)}"));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            SupplierOrder order = dto.ToSupplierOrder();

            _supplierOrderRepo.Add(order);
            await _supplierOrderRepo.SaveChangesAsync(ct);

            return order.SupplierOrderId;
        }

        public async Task<bool> UpdateAsync(UpdateSupplierOrderDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var errors = new List<(string Key, string Message)>();

            SupplierOrder? existingOrder = await _supplierOrderRepo.GetByIdAsync(dto.OrderId, includeItems: true, ct);
            if (existingOrder == null)
            {
                errors.Add((string.Empty, "Order not found."));
                throw new DomainValidationException(errors);
            }

            if (string.IsNullOrWhiteSpace(dto.OrderNumber))
                errors.Add((nameof(dto.OrderNumber), "Order number is required."));

            dto.Items ??= new List<UpdateSupplierOrderItemDto>();

            Supplier? supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, false, false, ct);
            if (supplier == null)
                errors.Add((nameof(dto.SupplierId), "Supplier is required."));

            var productIds = dto.Items.Select(i => i.ProductId).Distinct();
            var products = new List<Product>();
            foreach (var id in productIds)
            {
                var product = await _productRepo.GetByIdAsync(id, ct);
                if (product != null) products.Add(product);
            }
            var productsDict = products.ToDictionary(p => p.ProductId);

            foreach (var item in dto.Items)
            {
                if (!productsDict.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add((nameof(item.ProductId), $"Product not found: {item.ProductId}"));
                    continue;
                }
                if (product.SupplierId != dto.SupplierId)
                {
                    errors.Add((nameof(item.ProductId), $"Product {product.Name} does not belong to the selected supplier."));
                    continue;
                }
                if (item.Quantity < 1) errors.Add((nameof(item.Quantity), $"Quantity for product {product.Name} must be at least 1."));
                if (item.UnitPrice <= 0) errors.Add((nameof(item.UnitPrice), $"Unit price for product {product.Name} must be greater than 0."));
            }

            var duplicateIds = dto.Items.GroupBy(i => i.ProductId)
                                        .Where(g => g.Count() > 1)
                                        .Select(g => g.Key)
                                        .ToList();
            if (duplicateIds.Any())
            {
                var duplicateNames = duplicateIds.Select(id => productsDict[id].Name);
                errors.Add((nameof(UpdateSupplierOrderItemDto.ProductId), $"Duplicate products: {string.Join(", ", duplicateNames)}"));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            existingOrder.OrderNumber = dto.OrderNumber.Trim();
            existingOrder.Supplier = null;
            existingOrder.SupplierId = dto.SupplierId;
            existingOrder.OrderDate = dto.OrderDate;
            existingOrder.Notes = dto.Notes?.Trim();
            existingOrder.OrderStatus = dto.OrderStatus;

            // Sync items
            var existingItems = existingOrder.Items.ToDictionary(x => x.OrderItemId);
            var incomingIds = dto.Items.Select(x => x.ItemId).ToHashSet();

            foreach (var item in dto.Items)
            {
                if (existingItems.TryGetValue(item.ItemId, out SupplierOrderItem? ei))
                {
                    ei.OrderItemNumber = item.ItemNumber;
                    ei.Product = null;
                    ei.ProductId = item.ProductId;
                    ei.Quantity = item.Quantity;
                    ei.UnitPrice = item.UnitPrice;
                    existingItems.Remove(item.ItemId);
                }
                else
                {
                    _supplierOrderRepo.AddItem(new SupplierOrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        SupplierOrderId = existingOrder.SupplierOrderId,
                        OrderItemNumber = item.ItemNumber,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            foreach (var obsoleteItem in existingItems.Values)
            {
                _supplierOrderRepo.RemoveItem(obsoleteItem);
            }

            _supplierOrderRepo.Update(existingOrder);
            await _supplierOrderRepo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid orderId, CancellationToken ct = default)
        {
            SupplierOrder? existingSupplierOrder = await _supplierOrderRepo.GetByIdAsync(orderId, includeItems: false, ct);
            if (existingSupplierOrder is null) return false;

            _supplierOrderRepo.Remove(existingSupplierOrder);
            await _supplierOrderRepo.SaveChangesAsync(ct);
            return true;
        }

    }
}
