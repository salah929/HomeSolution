using System;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class UpdateSupplierDto
    {
        [Required(ErrorMessage = "Supplier name is required.")]
        [DataType(DataType.Text)]
        public Guid SupplierId { get; set; }

        [Required]
        [StringLength(40)]
        [DataType(DataType.Text)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [DataType(DataType.Text)]
        public string? PhoneNumber { get; set; }

        [StringLength(40)]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [StringLength(100)]
        [DataType(DataType.MultilineText)]
        public string? Address { get; set; }

        [StringLength(40)]
        [DataType(DataType.Text)]
        public string? ContactPerson { get; set; }
    }
}
