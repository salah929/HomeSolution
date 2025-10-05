using Home.DTOs;
using Home.Shared.Enums;

namespace Home.Web.ViewModels
{
    public class SupplierOrdersViewModel
    {
        public IEnumerable<SupplierOrderDto> SupplierOrders { get; set; } = new List<SupplierOrderDto>();

        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public string? SearchString { get; set; }
        public string? SearchBy { get; set; }
        public string? SortBy { get; set; }
        public bool Desc { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public SupplierOrderStatus? Status { get; set; } // required for search
    }
}
