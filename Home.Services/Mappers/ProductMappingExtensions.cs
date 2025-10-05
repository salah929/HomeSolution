using Home.DTOs;
using Home.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Services.Mappers
{
    public static class ProductMappingExtensions
    {
        public static ProductDto ToProductDto(this Product p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            return new ProductDto
            {
                ProductId = p.ProductId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier?.Name ?? string.Empty
            };
        }

        public static CreateProductDto ToCreateProductDto(this ProductDto p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            return new CreateProductDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                SupplierId = p.SupplierId
            };
        }
    }
}
