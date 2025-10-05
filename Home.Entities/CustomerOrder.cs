using System.ComponentModel.DataAnnotations;
using Home.Shared.Enums;

namespace Home.Entities
{
    public class CustomerOrder
    {
        [Key]
        public Guid CustomerOrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = "";

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; } = null!;

        public ICollection<CustomerOrderItem> Items { get; set; } = new List<CustomerOrderItem>();

        [StringLength(200)]
        public string? Notes { get; set; }

        public CustomerOrderStatus OrderStatus { get; set; } = CustomerOrderStatus.Pending;
    }
}
