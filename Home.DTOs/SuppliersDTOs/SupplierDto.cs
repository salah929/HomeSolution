using System;

namespace Home.DTOs
{
    public class SupplierDto
    {
        public Guid SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public ICollection<SupplierOrderDto> Orders { get; set; } = new List<SupplierOrderDto>();
        public ICollection<ProductDto> Products { get; set; } = new List<ProductDto>();
    }
}
