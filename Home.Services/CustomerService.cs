using Home.DTOs;
using Home.Entities;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;

namespace Home.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;

        public CustomerService(ICustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        public async Task<CustomerDto?> GetByIdAsync(Guid customerId, bool includeOrders, CancellationToken ct = default)
        {
            Customer? customer = await _customerRepo.GetByIdAsync(customerId, includeOrders, ct);
            return customer is null ? null : customer.ToCustomerDto();
        }

        public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Customer> customers = await _customerRepo.GetAllAsync(ct);
            return customers.Select(c => c.ToCustomerDto()).ToList();
        }

        public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string? searchString = null, string? searchBy = null,
                                                                  string? sortBy = null, bool desc = false, int page = 1, int pageSize = 20,
                                                                  CancellationToken ct = default)
        {
            IReadOnlyList<Customer> customers = await _customerRepo.SearchAsync(searchString, searchBy, sortBy, desc, page, pageSize, ct);
            return customers.Select(c => c.ToCustomerDto()).ToList();
        }

        public Task<int> CountAsync(string? searchString = null, string? searchBy = null, CancellationToken ct = default)
            => _customerRepo.CountAsync(searchString, searchBy, ct);

        public async Task<IReadOnlyList<CustomerOrderDto>> GetOrdersByCustomerAsync(Guid customerId, bool includeItems = false,
                                                                                    CancellationToken ct = default)
        {
            IReadOnlyList<CustomerOrder> orders = await _customerRepo.GetOrdersByCustomerAsync(customerId, includeItems, ct);
            return orders.Select(o => o.ToCustomerOrderDto(includeItems)).ToList();
        }

        public async Task<Guid> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            if (dto == null) errors.Add((string.Empty, "Customer data is required."));
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

            Customer customer = dto!.ToCustomer();

            _customerRepo.Add(customer);
            await _customerRepo.SaveChangesAsync(ct);
            return customer.CustomerId;
        }

        public async Task<bool> UpdateAsync(UpdateCustomerDto dto, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();

            if (dto == null) errors.Add((string.Empty, "Customer data is required."));
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

            Customer? existingCustomer = null;
            if (dto != null)
            {
                existingCustomer = await _customerRepo.GetByIdAsync(dto!.CustomerId, false, ct);
                if (existingCustomer is null) errors.Add((nameof(dto.CustomerId), "Customer not found."));
            }

            if (errors.Any()) throw new DomainValidationException(errors);

            existingCustomer!.Name = dto!.Name.Trim();
            existingCustomer.PhoneNumber = dto.PhoneNumber?.Trim();
            existingCustomer.Email = dto.Email?.Trim();
            existingCustomer.Address = dto.Address?.Trim();
            existingCustomer.ContactPerson = dto.ContactPerson?.Trim();

            _customerRepo.Update(existingCustomer);
            await _customerRepo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid customerId, CancellationToken ct = default)
        {
            var errors = new List<(string Key, string Message)>();
            if (customerId == Guid.Empty) errors.Add((string.Empty, "Invalid customer."));
            Customer? customer = await _customerRepo.GetByIdAsync(customerId, false, ct);
            if (customer is null) errors.Add((string.Empty, "Invalid customer."));
            if (errors.Any()) throw new DomainValidationException(errors);

            // Respect Restrict: prevent delete if customer has orders
            bool hasOrders = (await _customerRepo.GetOrdersByCustomerAsync(customerId, false, ct)).Any();
            if (hasOrders) return false;

            await _customerRepo.Remove(customerId, ct);
            await _customerRepo.SaveChangesAsync(ct);
            return true;
        }
    }
}
