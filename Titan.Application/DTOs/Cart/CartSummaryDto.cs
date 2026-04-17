using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Cart
{
    public record CartSummaryDto
    {
        public List<CartItemDto> Items { get; init; } = new();
        public int TotalItems { get; init; }
        public decimal SubTotal { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal ShippingCost { get; init; }
        public decimal TotalAmount { get; init; }
        public string? AppliedCouponCode { get; init; }
    }
}
