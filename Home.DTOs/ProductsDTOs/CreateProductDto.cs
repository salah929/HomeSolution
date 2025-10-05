using System;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product code is required.")]
        [StringLength(20)]
        [DataType(DataType.Text)]
        public string Code { get; set; } = string.Empty;

        [StringLength(40)]
        [DataType(DataType.Text)]
        public string? Name { get; set; }

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Invalid supplier")]
        [DataType(DataType.Text)]
        public Guid SupplierId { get; set; }
    }
}
