using Home.DTOs;

namespace Home.Web.ViewModels
{
    public class CustomersViewModel
    {
        public IEnumerable<CustomerDto> Customers { get; set; } = new List<CustomerDto>();

        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public string? SearchString { get; set; }
        public string? SearchBy { get; set; }
        public string? SortBy { get; set; }
        public bool Desc { get; set; }
    }
}
