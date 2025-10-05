using Home.DTOs;
using Home.Shared.Enums;

namespace Home.ServiceContracts
{
    public interface ICustomerOrderService
    {
        Task<CustomerOrderDto?> GetByIdAsync(Guid orderId, bool includeItems = false, CancellationToken ct = default);

        Task<IReadOnlyList<CustomerOrderDto>> SearchAsync(string? searchString, string? searchBy, Guid? customerId = null,
                                                          CustomerOrderStatus? status = null, DateTime? from = null, DateTime? to = null,
                                                          string? sortBy = null, bool desc = false,
                                                          int page = 1, int pageSize = 20, CancellationToken ct = default);

        Task<int> CountAsync(string? searchString, string? searchBy, Guid? customerId = null, CustomerOrderStatus? status = null,
                             DateTime? from = null, DateTime? to = null, CancellationToken ct = default);

        Task<Guid> CreateAsync(CreateCustomerOrderDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(UpdateCustomerOrderDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid orderId, CancellationToken ct = default);
    }
}


/*
 * In repositories, we used Remove because it matches EF Core (DbSet.Remove(entity)), and repositories are close to the data-access layer.
 * In services, we switched to Delete because it’s more business-oriented language — 
 * services usually talk in terms of actions meaningful to the domain (delete a customer, delete a product).
 * 
 * 👉 In short:
 *    Repository → technical/data operation → Remove.
 *    Service → business operation → Delete.
 */


/*
 * In the repository, we had two overloads:
 * void Remove(Product product);
 * Task Remove(Guid productId, CancellationToken ct = default);
 * 
 * But in the service, we only kept:
 * Task<bool> DeleteAsync(Guid productId, CancellationToken ct = default);
 * 
 * Why?
 * 
 * In the service layer, you usually delete by ID, because that’s what comes from the UI or API.
 * Passing a full entity (Product) is less common at the service level — by then, you already know the ID, not an EF-tracked entity.
 * 
 * 👉 So we simplified it to one method.
 */