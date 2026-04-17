using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Coupon
{
    public record CouponDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DiscountType DiscountType { get; init; }
        public decimal DiscountValue { get; init; }
        public decimal? MinimumOrderAmount { get; init; }
        public decimal? MaximumDiscountAmount { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public int? MaxUsageCount { get; init; }
        public int UsageCount { get; init; }
        public bool IsActive { get; init; }
        public bool IsExpired { get; init; }
        public int? RemainingUses { get; init; }
    }
}
