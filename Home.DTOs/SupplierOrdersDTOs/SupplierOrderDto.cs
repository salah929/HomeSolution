using Home.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Home.DTOs
{
    public class SupplierOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public SupplierOrderStatus OrderStatus { get; set; }
        public List<SupplierOrderItemDto> Items { get; set; } = new();
    }

    public class SupplierOrderItemDto
    {
        public Guid OrderItemId { get; set; }
        public int ItemNumber { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
