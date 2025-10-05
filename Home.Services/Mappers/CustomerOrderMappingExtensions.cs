using Home.DTOs;
using Home.Entities;

namespace Home.Services.Mappers
{
    public static class CustomerOrderMappingExtensions
    {
        public static UpdateCustomerOrderDto ToUpdateCustomerOrderDto(this CustomerOrderDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new UpdateCustomerOrderDto
            {
                OrderId = dto.OrderId,
                OrderNumber = dto.OrderNumber,
                OrderDate = dto.OrderDate,
                CustomerId = dto.CustomerId,
                OrderStatus = dto.OrderStatus,
                Notes = dto.Notes,
                Items = dto.Items?.Select(i => new UpdateCustomerOrderItemDto
                {
                    ItemId = i.ItemId,
                    ItemNumber = i.ItemNumber,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<UpdateCustomerOrderItemDto>()
            };
        }

        public static CreateCustomerOrderDto ToCreateCustomerOrderDto(this CustomerOrderDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new CreateCustomerOrderDto
            {
                OrderId = dto.OrderId,
                OrderNumber = dto.OrderNumber,
                OrderDate = dto.OrderDate,
                CustomerId = dto.CustomerId,
                OrderStatus = dto.OrderStatus,
                Notes = dto.Notes,
                Items = dto.Items?.Select(i => new CreateCustomerOrderItemDto
                {
                    ItemId = i.ItemId,
                    ItemNumber = i.ItemNumber,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<CreateCustomerOrderItemDto>()
            };
        }

        public static CustomerOrderDto ToCustomerOrderDto(this CustomerOrder o, bool includeItems)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            return new CustomerOrderDto
            {
                OrderId = o.CustomerOrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.Name ?? string.Empty,
                Notes = o.Notes,
                OrderStatus = o.OrderStatus,
                Items = includeItems
                    ? o.Items?.Select(orderItem => new CustomerOrderItemDto
                    {
                        ItemId = orderItem.OrderItemId,
                        ItemNumber = orderItem.OrderItemNumber,
                        ProductId = orderItem.ProductId,
                        ProductCode = orderItem.Product?.Code ?? string.Empty,
                        ProductName = orderItem.Product?.Name,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice
                    }).ToList() ?? new List<CustomerOrderItemDto>()
                    : new List<CustomerOrderItemDto>()
            };
        }

        public static CustomerOrder ToCustomerOrder(this CreateCustomerOrderDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new CustomerOrder
            {
                CustomerOrderId = dto.OrderId != Guid.Empty ? dto.OrderId : Guid.NewGuid(),
                OrderNumber = dto.OrderNumber?.Trim() ?? string.Empty,
                OrderDate = dto.OrderDate,
                CustomerId = dto.CustomerId,
                Notes = dto.Notes?.Trim(),
                OrderStatus = dto.OrderStatus,
                Items = dto.Items?.Select(i => new CustomerOrderItem
                {
                    OrderItemId = i.ItemId,
                    OrderItemNumber = i.ItemNumber,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<CustomerOrderItem>()
            };
        }
    }
}
