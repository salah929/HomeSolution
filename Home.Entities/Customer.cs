using System.ComponentModel.DataAnnotations;

namespace Home.Entities
{
    public class Customer
    {
        [Key]
        public Guid CustomerId { get; set; }

        [Required]
        [StringLength(40)]
        public string Name { get; set; } = "";

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(40)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(40)]
        public string? ContactPerson { get; set; }

        public ICollection<CustomerOrder> Orders { get; set; } = new List<CustomerOrder>();
    }
}
