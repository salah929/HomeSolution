using Home.DTOs;
using Home.Shared.Enums;

namespace Home.Web.ViewModels
{
    public class CustomerOrdersViewModel
    {
        public IEnumerable<CustomerOrderDto> CustomerOrders { get; set; } = new List<CustomerOrderDto>();

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

        public CustomerOrderStatus? Status { get; set; } // required for search
    }
}
