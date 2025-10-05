using System;

namespace Home.DTOs
{
    public class ProductDto
    {
        public Guid ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }

        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty; // extra info from Supplier
    }
}
