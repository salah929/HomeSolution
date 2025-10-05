using Home.DTOs;
using Home.Entities;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Home.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ISupplierRepository _supplierRepo;

        public ProductService(IProductRepository productRepo, ISupplierRepository supplierRepo)
        {
            _productRepo = productRepo;
            _supplierRepo = supplierRepo;
        }

        public async Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken ct = default)
        {
            Product? product = await _productRepo.GetByIdAsync(productId, ct);
            return product is null ? null : product.ToProductDto();
        }

        public async Task<ProductDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            Product? product = await _productRepo.GetByCodeAsync(code, ct);
            return product is null ? null : product.ToProductDto();
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Product> products = await _productRepo.GetAllAsync(ct);
            return products.Select(p => p.ToProductDto()).ToList();
            // or: return products.Select(p => ToDto(p)).ToList();
            // Alternatively, use LINQ query syntax:
            // return (from p in products select ToDto(p)).ToList();
        }

        public async Task<IReadOnlyList<ProductDto>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        {
            IReadOnlyList<Product> products = await _productRepo.GetBySupplierAsync(supplierId, ct);
            return products.Select(p => p.ToProductDto()).ToList();
        }

        public async Task<IReadOnlyList<ProductDto>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                                 Guid? supplierId = null, string? sortBy = null, bool desc = false,
                                                                 int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            IReadOnlyList<Product> products = await _productRepo.SearchAsync(searchString, searchBy, supplierId, sortBy, desc, page, pageSize, ct);
            return products.Select(p => p.ToProductDto()).ToList();
        }

        public Task<int> CountAsync(string? searchString = null, string? searchBy = null, Guid? supplierId = null,
                                    CancellationToken ct = default)
            => _productRepo.CountAsync(searchString, searchBy, supplierId, ct);

        public async Task<Guid> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            // Validate supplierId
            if (dto.SupplierId == Guid.Empty) errors.Add((nameof(dto.SupplierId), "Invalid supplier."));

            // Validate supplier
            else
            {
                Supplier? supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, false, false, ct);
                if (supplier is null) errors.Add((nameof(dto.SupplierId), "Invalid supplier."));
            }

            // Enforce unique code
            if (await _productRepo.CodeExistsAsync(dto.Code, null, ct))
                errors.Add((nameof(dto.Code), "Product code already exists."));

            if (errors.Any()) throw new DomainValidationException(errors);

            Product product = new Product
            {
                ProductId = Guid.NewGuid(),
                Code = dto.Code.Trim(),
                Name = dto.Name?.Trim(),
                Description = dto.Description?.Trim(),
                SupplierId = dto.SupplierId,
                // Supplier = supplier // optional, EF will link by SupplierId
            };

            try
            {
                _productRepo.Add(product);
                await _productRepo.SaveChangesAsync(ct);
                return product.ProductId;
            }
            // SQL Server unique index/constraint violations: 2601 or 2627
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                errors.Add((nameof(product.Code), "Product code already exists."));
                throw new DomainValidationException(errors);
            }
            catch (Exception)
            {
                errors.Add((string.Empty, "Unknown Error."));
                throw new DomainValidationException(errors);
            }
        }

        public async Task<bool> UpdateAsync(UpdateProductDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            // Validate dto
            if (dto.ProductId == Guid.Empty) errors.Add((nameof(dto.ProductId), "Invalid product."));

            // Validate exists
            Product? product = await _productRepo.GetByIdAsync(dto.ProductId, ct);
            if (product is null) errors.Add((nameof(dto.ProductId), "Invalid product."));

            // Validate supplierId
            if (dto.SupplierId == Guid.Empty) errors.Add((nameof(dto.SupplierId), "Invalid supplier."));

            // Validate supplier
            else
            {
                Supplier? supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, false, false, ct);
                if (supplier is null) errors.Add((nameof(dto.SupplierId), "Invalid supplier."));
            }

            // Enforce unique code (exclude current)
            if (await _productRepo.CodeExistsAsync(dto.Code, dto.ProductId, ct))
                errors.Add((nameof(dto.Code), "Product code already exists."));

            if (errors.Any()) throw new DomainValidationException(errors);

            product!.Code = dto.Code.Trim();
            product.Name = dto.Name?.Trim();
            product.Description = dto.Description?.Trim();
            product.SupplierId = dto.SupplierId;
            product.Supplier = null; // important - avoid nav overriding the FK

            try
            {
                _productRepo.Update(product);
                await _productRepo.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception)
            {
                errors.Add((string.Empty, "Unknown Error."));
                throw new DomainValidationException(errors);
            }
            
        }

        public async Task<bool> DeleteAsync(Guid productId, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();
            if (productId == Guid.Empty) errors.Add((nameof(productId), "Invalid product."));
            Product? product = await _productRepo.GetByIdAsync(productId, ct);
            if (product is null) errors.Add((nameof(productId), "Invalid product."));
            if (errors.Any()) throw new DomainValidationException(errors);

            try
            {
                _productRepo.Remove(product!);
                await _productRepo.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception)
            {
                errors.Add((string.Empty, "Unknown Error."));
                throw new DomainValidationException(errors);
            }
        }

        public Task<bool> CodeExistsAsync(string code, Guid? excludeProductId = null, CancellationToken ct = default)
            => _productRepo.CodeExistsAsync(code, excludeProductId, ct);

        public Task<List<CustomerOrder>> GetCustomerOrdersByProductId(Guid orderItemId, CancellationToken ct = default)
        {
            return _productRepo.GetCustomerOrdersByProductId(orderItemId, ct); throw new NotImplementedException();
        }

        public Task<List<SupplierOrder>> GetSupplierOrdersByProductId(Guid orderItemId, CancellationToken ct = default)
        {
            return _productRepo.GetSupplierOrdersByProductId(orderItemId, ct); throw new NotImplementedException();
        }
    }
}
