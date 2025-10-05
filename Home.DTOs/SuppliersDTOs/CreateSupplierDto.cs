using System;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(40)]
        [DataType(DataType.Text)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [DataType(DataType.Text)]
        [Display(Name = "Phone Number")]
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
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }
    }
}
