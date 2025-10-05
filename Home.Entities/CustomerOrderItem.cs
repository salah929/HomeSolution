using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Entities
{
    public class CustomerOrderItem
    {
        [Key]
        public Guid OrderItemId { get; set; }

        public int OrderItemNumber { get; set; }

        [Required]
        public Guid CustomerOrderId { get; set; }
        public CustomerOrder CustomerOrder { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }
        public Product? Product { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
