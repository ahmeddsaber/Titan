using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Cart;
using Titan.Application.DTOs.Coupon;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services;

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICouponService _couponService;

        public CartService(ApplicationDbContext db, ICouponService couponService)
        {
            _db = db;
            _couponService = couponService;
        }

        public async Task<ApiResponse<CartSummaryDto>> GetCartAsync(Guid userId, string? couponCode = null)
        {
            var items = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p.Images)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            var cartItems = items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductSlug = i.Product.Slug,
                ProductImageUrl = i.Product.MainImageUrl,
                UnitPrice = i.Product.CurrentPrice,
                Quantity = i.Quantity,
                TotalPrice = i.Product.CurrentPrice * i.Quantity,
                StockQuantity = i.Product.StockQuantity,
                VariantId = i.VariantId,
                VariantInfo = i.Variant != null ? $"{i.Variant.Size} / {i.Variant.Color}" : null
            }).ToList();

            var subTotal = cartItems.Sum(i => i.TotalPrice);
            decimal discount = 0;
            string? appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var couponResult = await ValidateCouponAsync(couponCode, subTotal);
                if (couponResult.Success && couponResult.Data != null)
                {
                    discount = await _couponService.CalculateDiscountAsync(couponResult.Data.Id, subTotal);
                    appliedCoupon = couponCode.ToUpper();
                }
            }

            var shipping = subTotal > 500 ? 0 : 50;
            return ApiResponse<CartSummaryDto>.Ok(new CartSummaryDto
            {
                Items = cartItems,
                TotalItems = cartItems.Sum(i => i.Quantity),
                SubTotal = subTotal,
                DiscountAmount = discount,
                ShippingCost = shipping,
                TotalAmount = subTotal - discount + shipping,
                AppliedCouponCode = appliedCoupon
            });
        }

        public async Task<ApiResponse<CartItemDto>> AddToCartAsync(Guid userId, AddToCartDto dto)
        {
            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null) return ApiResponse<CartItemDto>.Fail("Product not found.");
            if (!product.IsActive) return ApiResponse<CartItemDto>.Fail("Product is not available.");
            if (product.StockQuantity < dto.Quantity) return ApiResponse<CartItemDto>.Fail($"Only {product.StockQuantity} items in stock.");

            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId && c.VariantId == dto.VariantId);
            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
                if (existing.Quantity > product.StockQuantity) existing.Quantity = product.StockQuantity;
            }
            else
            {
                existing = new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.CurrentPrice
                };
                _db.CartItems.Add(existing);
            }
            await _db.SaveChangesAsync();
            return ApiResponse<CartItemDto>.Ok(new CartItemDto
            {
                Id = existing.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                ProductImageUrl = product.MainImageUrl,
                UnitPrice = product.CurrentPrice,
                Quantity = existing.Quantity,
                TotalPrice = product.CurrentPrice * existing.Quantity,
                StockQuantity = product.StockQuantity
            }, "Added to cart.");
        }

        public async Task<ApiResponse<CartItemDto>> UpdateQuantityAsync(Guid userId, UpdateCartItemDto dto)
        {
            var item = await _db.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == dto.CartItemId && c.UserId == userId);
            if (item == null) return ApiResponse<CartItemDto>.Fail("Cart item not found.");
            if (dto.Quantity <= 0)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
                return ApiResponse<CartItemDto>.Ok(null!, "Item removed.");
            }
            if (dto.Quantity > item.Product.StockQuantity) return ApiResponse<CartItemDto>.Fail($"Only {item.Product.StockQuantity} in stock.");
            item.Quantity = dto.Quantity;
            await _db.SaveChangesAsync();
            return ApiResponse<CartItemDto>.Ok(new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductSlug = item.Product.Slug,
                ProductImageUrl = item.Product.MainImageUrl,
                UnitPrice = item.Product.CurrentPrice,
                Quantity = item.Quantity,
                TotalPrice = item.Product.CurrentPrice * item.Quantity,
                StockQuantity = item.Product.StockQuantity
            });
        }

        public async Task<ApiResponse<bool>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) return ApiResponse<bool>.Fail("Item not found.");
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<bool>> ClearCartAsync(Guid userId)
        {
            var items = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<CouponDto>> ValidateCouponAsync(string code, decimal orderTotal)
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper() && c.IsActive);
            if (coupon == null) return ApiResponse<CouponDto>.Fail("Invalid coupon code.");
            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow) return ApiResponse<CouponDto>.Fail("Coupon has expired.");
            if (coupon.MaxUsageCount.HasValue && coupon.UsageCount >= coupon.MaxUsageCount) return ApiResponse<CouponDto>.Fail("Coupon usage limit reached.");
            if (coupon.MinimumOrderAmount.HasValue && orderTotal < coupon.MinimumOrderAmount) return ApiResponse<CouponDto>.Fail($"Minimum order amount is {coupon.MinimumOrderAmount:C}.");
            return ApiResponse<CouponDto>.Ok(MapCoupon(coupon), "Coupon applied successfully!");
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


