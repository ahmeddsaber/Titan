namespace Titan.Client.Services.Models
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CouponCode { get; set; }
        public string ShippingFullName { get; set; } = "";
        public string ShippingPhone { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string ShippingCity { get; set; } = "";
        public string ShippingCountry { get; set; } = "";
        public string? Notes { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }
}
