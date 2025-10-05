using Home.DTOs;
using Home.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Services.Mappers
{
    public static class SupplierMappingExtenstions
    {
        public static SupplierDto ToSupplierDto(this Supplier supplier) => new()
        {
            SupplierId = supplier.SupplierId,
            Name = supplier.Name,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            ContactPerson = supplier.ContactPerson,
            Products = supplier.Products?.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                SupplierId = p.SupplierId,
                SupplierName = supplier.Name
            }).ToList() ?? new List<ProductDto>(),
            Orders = supplier.Orders?.Select(o => new SupplierOrderDto
            {
                OrderId = o.SupplierOrderId,
                OrderDate = o.OrderDate,
                OrderNumber = o.OrderNumber,
                OrderStatus = o.OrderStatus
            }).ToList() ?? new List<SupplierOrderDto>()
        };

        public static UpdateSupplierDto ToUpdateSupplierDto(this SupplierDto supplierDto)
        {
            if (supplierDto == null) throw new ArgumentNullException(nameof(supplierDto));
            return new UpdateSupplierDto
            {
                SupplierId = supplierDto.SupplierId,
                Name = supplierDto.Name,
                Email = supplierDto.Email,
                PhoneNumber = supplierDto.PhoneNumber,
                Address = supplierDto.Address,
                ContactPerson = supplierDto.ContactPerson
            };
        }
    }
}
