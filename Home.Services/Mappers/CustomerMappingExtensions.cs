using Home.DTOs;
using Home.Entities;

namespace Home.Services.Mappers
{
    public static class CustomerMappingExtensions
    {
        public static CustomerDto ToCustomerDto(this Customer c)
        { 
            if (c == null) throw new ArgumentNullException(nameof(c));
            return new CustomerDto
            {
                CustomerId = c.CustomerId,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                ContactPerson = c.ContactPerson,
                Orders = c.Orders?.Select(order => new CustomerOrderDto
                {
                    OrderId = order.CustomerOrderId,
                    OrderDate = order.OrderDate,
                    OrderNumber = order.OrderNumber,
                    OrderStatus = order.OrderStatus
                }).ToList() ?? new List<CustomerOrderDto>()
            };
        }

        public static UpdateCustomerDto ToUpdateCustomerDto(this CustomerDto customerDto)
        {
            if (customerDto == null) throw new ArgumentNullException(nameof(customerDto));
            return new UpdateCustomerDto
            {
                CustomerId = customerDto.CustomerId,
                Name = customerDto.Name,
                Email = customerDto.Email,
                PhoneNumber = customerDto.PhoneNumber,
                Address = customerDto.Address,
                ContactPerson = customerDto.ContactPerson
            };
        }

        public static Customer ToCustomer(this CreateCustomerDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new Customer
            {
                CustomerId = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                ContactPerson = dto.ContactPerson?.Trim()
            };
        }
    }
}
