using Home.DTOs;
using Home.Entities;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;

namespace Home.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepo;

        public SupplierService(ISupplierRepository supplierRepo)
        {
            _supplierRepo = supplierRepo;
        }

        public async Task<SupplierDto?> GetByIdAsync(Guid supplierId, bool includeOrders = false, bool includeProducts = false, CancellationToken ct = default)
        {
            Supplier? supplier = await _supplierRepo.GetByIdAsync(supplierId, includeOrders, includeProducts, ct);
            return supplier is null ? null : supplier.ToSupplierDto();
        }

        public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Supplier> suppliers = await _supplierRepo.GetAllAsync(ct);
            return suppliers.Select(s => s.ToSupplierDto()).ToList();
        }

        public async Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                                  string? sortBy = null, bool desc = false, int page = 1, int pageSize = 20,
                                                                  CancellationToken ct = default)
        {
            IReadOnlyList<Supplier> suppliers = await _supplierRepo.SearchAsync(searchString, searchBy, sortBy, desc, page, pageSize, ct);
            return suppliers.Select(s => s.ToSupplierDto()).ToList();
        }

        public Task<int> CountAsync(string? searchString = null, string? searchBy = null, CancellationToken ct = default)
            => _supplierRepo.CountAsync(searchString, searchBy, ct);

        public async Task<IReadOnlyList<SupplierOrderDto>> GetOrdersBySupplierAsync(Guid supplierId, bool includeItems = false, CancellationToken ct = default)
        {
            IReadOnlyList<SupplierOrder> orders = await _supplierRepo.GetOrdersBySupplierAsync(supplierId, includeItems, ct);
            return orders.Select(o => o.ToSupplierOrderDto(includeItems)).ToList();
        }

        //public async Task<IReadOnlyList<ProductDto>> GetProductsBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        //{
        //    Supplier? supplier = await _supplierRepo.GetByIdAsync(supplierId, true, ct);
        //    return supplier?.Products.Select(p => new ProductDto
        //    {
        //        ProductId = p.ProductId,
        //        Code = p.Code,
        //        Name = p.Name,
        //        Description = p.Description,
        //        SupplierId = p.SupplierId,
        //        SupplierName = supplier.Name
        //    }).ToList() ?? new List<ProductDto>();
        //    //IReadOnlyList<Product> products = await _supplierRepo.GetProductsBySupplierAsync(supplierId, ct);
        //    //string supplierName = supplier?.Name ?? string.Empty;

        //    //return products.Select(p => new ProductDto
        //    //{
        //    //    ProductId = p.ProductId,
        //    //    Code = p.Code,
        //    //    Name = p.Name,
        //    //    Description = p.Description,
        //    //    SupplierId = p.SupplierId,
        //    //    SupplierName = supplierName
        //    //}).ToList();
        //    // or
        //    //return [.. products.Select(p => new ProductDto
        //    //{
        //    //    ProductId = p.ProductId,
        //    //    Code = p.Code,
        //    //    Name = p.Name,
        //    //    Description = p.Description,
        //    //    SupplierId = p.SupplierId,
        //    //    SupplierName = supplierName
        //    //})];
        //}

        public async Task<Guid> CreateAsync(CreateSupplierDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            if (dto == null) errors.Add((string.Empty, "Supplier data is required."));
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    errors.Add((nameof(dto.Name), "Name is required."));

                if (string.IsNullOrWhiteSpace(dto.Email))
                    errors.Add((nameof(dto.Email), "Email is required."));
                else
                {
                    var email = dto.Email.Trim();
                    try
                    {
                        var addr = new System.Net.Mail.MailAddress(email);
                        if (addr.Address != email)
                            errors.Add((nameof(dto.Email), "Invalid email format."));
                    }
                    catch
                    {
                        errors.Add((nameof(dto.Email), "Invalid email format."));
                    }
                }
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            Supplier supplier = new Supplier
            {
                SupplierId = Guid.NewGuid(),
                Name = dto!.Name.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                ContactPerson = dto.ContactPerson?.Trim()
            };

            _supplierRepo.Add(supplier);
            await _supplierRepo.SaveChangesAsync(ct);
            return supplier.SupplierId;
        }

        public async Task<bool> UpdateAsync(UpdateSupplierDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            if (dto == null) errors.Add((string.Empty, "Supplier data is required."));
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    errors.Add((nameof(dto.Name), "Name is required."));

                if (string.IsNullOrWhiteSpace(dto.Email))
                    errors.Add((nameof(dto.Email), "Email is required."));
                else
                {
                    var email = dto.Email.Trim();
                    try
                    {
                        var addr = new System.Net.Mail.MailAddress(email);
                        if (addr.Address != email)
                            errors.Add((nameof(dto.Email), "Invalid email format."));
                    }
                    catch
                    {
                        errors.Add((nameof(dto.Email), "Invalid email format."));
                    }
                }
            }

            Supplier? existingSupplier = null;
            if (dto != null)
            {
                existingSupplier = await _supplierRepo.GetByIdAsync(dto!.SupplierId, false, false, ct);
                if (existingSupplier is null) errors.Add((nameof(dto.SupplierId), "Supplier not found."));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            existingSupplier!.Name = dto!.Name.Trim();
            existingSupplier.PhoneNumber = dto.PhoneNumber?.Trim();
            existingSupplier.Email = dto.Email?.Trim();
            existingSupplier.Address = dto.Address?.Trim();
            existingSupplier.ContactPerson = dto.ContactPerson?.Trim();

            _supplierRepo.Update(existingSupplier);
            await _supplierRepo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid supplierId, CancellationToken ct = default)
        {
            // Respect Restrict: prevent delete if supplier has orders or products
            bool hasOrders = (await _supplierRepo.GetOrdersBySupplierAsync(supplierId, false, ct)).Any();
            bool hasProducts = (await _supplierRepo.GetProductsBySupplierAsync(supplierId, ct)).Any();
            if (hasOrders || hasProducts) return false;

            await _supplierRepo.Remove(supplierId, ct);
            await _supplierRepo.SaveChangesAsync(ct);
            return true;
        }
    }
}
