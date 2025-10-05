using Home.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Home.Web.ViewModels
{
    public class ProductsViewModel
    {
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();

        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // Optional: computed property for convenience
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Keep the search/sort context so UI can reuse it
        public string? SearchString { get; set; }
        public string? SearchBy { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SortBy { get; set; }
        public bool Desc { get; set; }
        public SelectList? SupplierList { get; set; }
    }
}
