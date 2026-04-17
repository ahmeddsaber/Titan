using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Cart;
using Titan.Application.DTOs.Coupon;

namespace Titan.Application.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<CartSummaryDto>> GetCartAsync(Guid userId, string? couponCode = null);
        Task<ApiResponse<CartItemDto>> AddToCartAsync(Guid userId, AddToCartDto dto);
        Task<ApiResponse<CartItemDto>> UpdateQuantityAsync(Guid userId, UpdateCartItemDto dto);
        Task<ApiResponse<bool>> RemoveFromCartAsync(Guid userId, Guid cartItemId);
        Task<ApiResponse<bool>> ClearCartAsync(Guid userId);
        Task<ApiResponse<CouponDto>> ValidateCouponAsync(string code, decimal orderTotal);
    }

}
