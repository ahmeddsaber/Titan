using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Coupon
{
    public record CreateCouponDto(string Code, string Description,
        DiscountType DiscountType, decimal DiscountValue, decimal? MinimumOrderAmount,
        decimal? MaximumDiscountAmount, DateTime? ExpiresAt, int? MaxUsageCount);
}
