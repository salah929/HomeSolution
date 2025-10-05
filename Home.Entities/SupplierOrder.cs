using Home.Entities;
using Home.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Home.Entities
{
    public class SupplierOrder
    {
        [Key]
        public Guid SupplierOrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = "";

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid SupplierId { get; set; }
        public Supplier? Supplier { get; set; } = null!;

        public ICollection<SupplierOrderItem> Items { get; set; } = new List<SupplierOrderItem>();

        [StringLength(200)]
        public string? Notes { get; set; }

        public SupplierOrderStatus OrderStatus { get; set; } = SupplierOrderStatus.Pending;
    }
}
