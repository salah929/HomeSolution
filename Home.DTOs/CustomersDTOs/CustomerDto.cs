using System;

namespace Home.DTOs
{
    public class CustomerDto
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }

        public ICollection<CustomerOrderDto> Orders { get; set; } = new List<CustomerOrderDto>();
    }
}
