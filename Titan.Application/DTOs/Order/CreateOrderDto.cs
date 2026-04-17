using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Order
{
    public record CreateOrderDto
    {
        public string ShippingFullName { get; init; } = string.Empty;
        public string ShippingPhone { get; init; } = string.Empty;
        public string ShippingAddress { get; init; } = string.Empty;
        public string ShippingCity { get; init; } = string.Empty;
        public string ShippingCountry { get; init; } = string.Empty;
        public string ShippingPostalCode { get; init; } = string.Empty;
        public string? CouponCode { get; init; }
        public string? Notes { get; init; }
    }

}
