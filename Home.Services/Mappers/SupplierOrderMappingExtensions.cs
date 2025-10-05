using Home.DTOs;
using Home.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Services.Mappers
{
    public static class SupplierOrderMappingExtensions
    {
        public static SupplierOrderDto ToSupplierOrderDto(this SupplierOrder o, bool includeItems)
        {
            return new SupplierOrderDto
            {
                OrderId = o.SupplierOrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier?.Name ?? string.Empty,
                Notes = o.Notes,
                OrderStatus = o.OrderStatus,
                Items = includeItems
                    ? o.Items?.Select(i => new SupplierOrderItemDto
                    {
                        OrderItemId = i.OrderItemId,
                        ItemNumber = i.OrderItemNumber,
                        ProductId = i.ProductId,
                        ProductCode = i.Product?.Code ?? string.Empty,
                        ProductName = i.Product?.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<SupplierOrderItemDto>()
                    : new List<SupplierOrderItemDto>()
            };
        }

        public static UpdateSupplierOrderDto ToUpdateSupplierOrderDto(this SupplierOrderDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new UpdateSupplierOrderDto
            {
                OrderId = dto.OrderId,
                OrderNumber = dto.OrderNumber,
                OrderDate = dto.OrderDate,
                SupplierId = dto.SupplierId,
                OrderStatus = dto.OrderStatus,
                Notes = dto.Notes,
                Items = dto.Items?.Select(i => new UpdateSupplierOrderItemDto
                {
                    ItemId = i.OrderItemId,
                    ItemNumber = i.ItemNumber,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<UpdateSupplierOrderItemDto>()
            };
        }

        public static SupplierOrder ToSupplierOrder(this CreateSupplierOrderDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new SupplierOrder
            {
                SupplierOrderId = dto.OrderId != Guid.Empty ? dto.OrderId : Guid.NewGuid(),
                OrderNumber = dto.OrderNumber?.Trim() ?? string.Empty,
                OrderDate = dto.OrderDate,
                SupplierId = dto.SupplierId,
                Notes = dto.Notes?.Trim(),
                OrderStatus = dto.OrderStatus,
                Items = dto.Items?.Select(i => new SupplierOrderItem
                {
                    OrderItemId = i.ItemId,
                    OrderItemNumber = i.ItemNumber,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<SupplierOrderItem>()
            };
        }

    }
}
