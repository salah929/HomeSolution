using Home.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Home.Web.ViewModels
{
    public class ProductViewModel
    {
        public Guid ProductId { get; set; }

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
        public SelectList? SupplierList { get; set; }

        public CreateProductDto ToCreateProductDto()
        {
            return new CreateProductDto
            {
                Code = this.Code,
                Name = this.Name,
                Description = this.Description,
                SupplierId = this.SupplierId
            };
        }
        public UpdateProductDto ToUpdateProductDto()
        {
            return new UpdateProductDto
            {
                ProductId = this.ProductId,
                Code = this.Code,
                Name = this.Name,
                Description = this.Description,
                SupplierId = this.SupplierId
            };
        }
    }
}
