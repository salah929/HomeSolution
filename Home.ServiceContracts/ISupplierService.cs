using Home.DTOs;

namespace Home.ServiceContracts
{
    public interface ISupplierService
    {
        Task<SupplierDto?> GetByIdAsync(Guid supplierId, bool includeOrders = false, bool includeIProducts = false, CancellationToken ct = default);
        Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default);

        Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                     string? sortBy = null, bool desc = false,
                                                     int page = 1, int pageSize = 20,
                                                     CancellationToken ct = default);

        Task<int> CountAsync(string? searchString = null, string? searchBy = null, CancellationToken ct = default);

        Task<IReadOnlyList<SupplierOrderDto>> GetOrdersBySupplierAsync(Guid supplierId, bool includeItems = false, CancellationToken ct = default);
        //Task<IReadOnlyList<ProductDto>> GetProductsBySupplierAsync(Guid supplierId, CancellationToken ct = default);

        Task<Guid> CreateAsync(CreateSupplierDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(UpdateSupplierDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid supplierId, CancellationToken ct = default);
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