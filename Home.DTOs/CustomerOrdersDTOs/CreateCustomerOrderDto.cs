using Home.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class CreateCustomerOrderDto
    {
        [DataType(DataType.Text)]
        public Guid OrderId { get; set; }

        [Required(ErrorMessage = "Invalid customer.")]
        [DataType(DataType.Text)]
        public Guid CustomerId { get; set; }

        public bool FromCustomer { get; set; } = false; // true when the view is called from Customer Details Page

        [Required(ErrorMessage = "Order number is required.")]
        [DataType(DataType.Text)]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = "";

        [Required(ErrorMessage = "Order date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Order status is required.")]
        [Display(Name = "Order Status")]
        public CustomerOrderStatus OrderStatus { get; set; }

        public List<CreateCustomerOrderItemDto> Items { get; set; } = new();
    }

    public class CreateCustomerOrderItemDto
    {
        [Required]
        [DataType(DataType.Text)]
        public Guid ItemId { get; set; }

        [Required(ErrorMessage = "Invalid order item number.")]
        [Range(1, int.MaxValue, ErrorMessage = "Order item number must be at least 1.")]
        public int ItemNumber { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Ivalid quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Invalid price.")]
        public decimal UnitPrice { get; set; }
    }
}
