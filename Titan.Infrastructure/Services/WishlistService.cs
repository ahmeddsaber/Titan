using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Product;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        public WishlistService(ApplicationDbContext db, IProductService productService) { _db = db; _productService = productService; }

        public async Task<ApiResponse<List<ProductDto>>> GetWishlistAsync(Guid userId)
        {
            var items = await _db.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product).ThenInclude(p => p.Category)
                .Include(w => w.Product).ThenInclude(p => p.Images)
                .Include(w => w.Product).ThenInclude(p => p.Variants)
                .AsNoTracking().ToListAsync();
            var products = items.Select(w => ProductService.MapProduct(w.Product, true, false)).ToList();
            return ApiResponse<List<ProductDto>>.Ok(products);
        }

        public async Task<ApiResponse<bool>> AddToWishlistAsync(Guid userId, Guid productId)
        {
            if (await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId))
                return ApiResponse<bool>.Ok(true, "Already in wishlist.");
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Added to wishlist.");
        }

        public async Task<ApiResponse<bool>> RemoveFromWishlistAsync(Guid userId, Guid productId)
        {
            var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            if (item == null) return ApiResponse<bool>.Fail("Not in wishlist.");
            _db.WishlistItems.Remove(item);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Removed from wishlist.");
        }

        public async Task<ApiResponse<bool>> IsInWishlistAsync(Guid userId, Guid productId)
        {
            var exists = await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
            return ApiResponse<bool>.Ok(exists);
        }
    }

}
