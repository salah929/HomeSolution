using Home.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class CustomerOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public CustomerOrderStatus OrderStatus { get; set; }
        public List<CustomerOrderItemDto> Items { get; set; } = new();
    }

    public class CustomerOrderItemDto
    {
        public Guid ItemId { get; set; }
        public int ItemNumber { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
