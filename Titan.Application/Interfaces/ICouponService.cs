using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Coupon;
using Titan.Application.DTOs.Pagination;

namespace Titan.Application.Interfaces
{
    public interface ICouponService
    {
        Task<ApiResponse<PagedResult<CouponDto>>> GetAllAsync(int page, int pageSize);
        Task<ApiResponse<CouponDto>> GetByCodeAsync(string code);
        Task<ApiResponse<CouponDto>> CreateAsync(CreateCouponDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
        Task<ApiResponse<bool>> ToggleActiveAsync(Guid id);
        Task<decimal> CalculateDiscountAsync(Guid couponId, decimal orderTotal);
    }
}
