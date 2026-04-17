using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Order
{
    public record OrderDto
    {
        public Guid Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public OrderStatus Status { get; init; }
        public string StatusLabel { get; init; } = string.Empty;
        public decimal SubTotal { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal ShippingCost { get; init; }
        public decimal TotalAmount { get; init; }
        public string? CouponCode { get; init; }
        public string ShippingFullName { get; init; } = string.Empty;
        public string ShippingPhone { get; init; } = string.Empty;
        public string ShippingAddress { get; init; } = string.Empty;
        public string ShippingCity { get; init; } = string.Empty;
        public string ShippingCountry { get; init; } = string.Empty;
        public string? Notes { get; init; }
        public DateTime? EstimatedDelivery { get; init; }
        public DateTime? DeliveredAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<OrderItemDto> Items { get; init; } = new();
        public List<OrderStatusHistoryDto> StatusHistory { get; init; } = new();
    }

}
