using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Coupon;
using Titan.Application.DTOs.Pagination;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Domain.Enum;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services;

    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _db;
        public CouponService(ApplicationDbContext db) { _db = db; }

        public async Task<ApiResponse<PagedResult<CouponDto>>> GetAllAsync(int page, int pageSize)
        {
            var query = _db.Coupons.OrderByDescending(c => c.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<CouponDto>>.Ok(new PagedResult<CouponDto>
            {
                Items = items.Select(MapCoupon).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<CouponDto>> GetByCodeAsync(string code)
        {
            var c = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper());
            return c == null ? ApiResponse<CouponDto>.Fail("Not found.") : ApiResponse<CouponDto>.Ok(MapCoupon(c));
        }

        public async Task<ApiResponse<CouponDto>> CreateAsync(CreateCouponDto dto)
        {
            if (await _db.Coupons.AnyAsync(c => c.Code == dto.Code.ToUpper()))
                return ApiResponse<CouponDto>.Fail("Coupon code already exists.");
            var coupon = new Coupon
            {
                Code = dto.Code.ToUpper(),
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinimumOrderAmount = dto.MinimumOrderAmount,
                MaximumDiscountAmount = dto.MaximumDiscountAmount,
                ExpiresAt = dto.ExpiresAt,
                MaxUsageCount = dto.MaxUsageCount
            };
            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();
            return ApiResponse<CouponDto>.Ok(MapCoupon(coupon));
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var c = await _db.Coupons.FindAsync(id);
            if (c == null) return ApiResponse<bool>.Fail("Not found.");
            _db.Coupons.Remove(c);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<bool>> ToggleActiveAsync(Guid id)
        {
            var c = await _db.Coupons.FindAsync(id);
            if (c == null) return ApiResponse<bool>.Fail("Not found.");
            c.IsActive = !c.IsActive;
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(c.IsActive);
        }

        public async Task<decimal> CalculateDiscountAsync(Guid couponId, decimal orderTotal)
        {
            var coupon = await _db.Coupons.FindAsync(couponId);
            if (coupon == null) return 0;
            var discount = coupon.DiscountType == DiscountType.Percentage
                ? orderTotal * (coupon.DiscountValue / 100)
                : coupon.DiscountValue;
            if (coupon.MaximumDiscountAmount.HasValue)
                discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
            return Math.Round(discount, 2);
        }

        private static CouponDto MapCoupon(Coupon c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Description = c.Description,
            DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue,
            MinimumOrderAmount = c.MinimumOrderAmount,
            MaximumDiscountAmount = c.MaximumDiscountAmount,
            ExpiresAt = c.ExpiresAt,
            MaxUsageCount = c.MaxUsageCount,
            UsageCount = c.UsageCount,
            IsActive = c.IsActive,
            IsExpired = c.ExpiresAt.HasValue && c.ExpiresAt < DateTime.UtcNow,
            RemainingUses = c.MaxUsageCount.HasValue ? c.MaxUsageCount - c.UsageCount : null
        };
    }

