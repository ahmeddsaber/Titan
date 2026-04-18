using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Product;
using Titan.Application.DTOs.Recommendation;
using Titan.Application.Interfaces;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        public RecommendationService(ApplicationDbContext db, IProductService productService) { _db = db; _productService = productService; }

        public async Task<ApiResponse<RecommendationDto>> GetRecommendationsAsync(Guid userId, int count = 8)
        {
            var viewedCategoryIds = await _db.ProductViews
                .Where(v => v.UserId == userId)
                .Include(v => v.Product)
                .Select(v => v.Product.CategoryId)
                .Distinct().ToListAsync();

            List<ProductDto> products;
            string reasonKey;

            if (viewedCategoryIds.Any())
            {
                var productList = await _db.Products
                    .Include(p => p.Category).Include(p => p.Images).Include(p => p.Variants)
                    .Where(p => p.IsActive && viewedCategoryIds.Contains(p.CategoryId))
                    .OrderByDescending(p => p.AverageRating).Take(count).AsNoTracking().ToListAsync();
                products = productList.Select(p => ProductService.MapProduct(p, false, false)).ToList();
                reasonKey = "based_on_views";
            }
            else
            {
                var productList = await _db.Products
                    .Include(p => p.Category).Include(p => p.Images).Include(p => p.Variants)
                    .Where(p => p.IsActive && p.IsFeatured).OrderByDescending(p => p.SoldCount).Take(count).AsNoTracking().ToListAsync();
                products = productList.Select(p => ProductService.MapProduct(p, false, false)).ToList();
                reasonKey = "trending";
            }

            return ApiResponse<RecommendationDto>.Ok(new RecommendationDto(products, reasonKey));
        }
    }
}
