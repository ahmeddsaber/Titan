using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Catogery;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.Product;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ProductService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<ApiResponse<PagedResult<ProductDto>>> GetAllAsync(ProductFilterDto filter, Guid? currentUserId)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p => p.Name.Contains(filter.Search) || p.NameAr.Contains(filter.Search) || p.SKU.Contains(filter.Search));

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.IsFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == filter.IsFeatured.Value);

            if (filter.HasDiscount.HasValue && filter.HasDiscount.Value)
                query = query.Where(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.Price);

            query = filter.SortBy switch
            {
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "popular" => query.OrderByDescending(p => p.SoldCount),
                "rating" => query.OrderByDescending(p => p.AverageRating),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var total = await query.CountAsync();
            var products = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

            HashSet<Guid> wishlistIds = new();
            HashSet<Guid> cartIds = new();
            if (currentUserId.HasValue)
            {
                wishlistIds = (await _db.WishlistItems.Where(w => w.UserId == currentUserId.Value).Select(w => w.ProductId).ToListAsync()).ToHashSet();
                cartIds = (await _db.CartItems.Where(c => c.UserId == currentUserId.Value).Select(c => c.ProductId).ToListAsync()).ToHashSet();
            }

            var dtos = products.Select(p => MapProduct(p, wishlistIds.Contains(p.Id), cartIds.Contains(p.Id))).ToList();

            return ApiResponse<PagedResult<ProductDto>>.Ok(new PagedResult<ProductDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var product = await GetProductQuery().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return ApiResponse<ProductDto>.Fail("Product not found.");
            return await BuildProductResponse(product, currentUserId);
        }

        public async Task<ApiResponse<ProductDto>> GetBySlugAsync(string slug, Guid? currentUserId)
        {
            var product = await GetProductQuery().FirstOrDefaultAsync(p => p.Slug == slug);
            if (product == null) return ApiResponse<ProductDto>.Fail("Product not found.");
            return await BuildProductResponse(product, currentUserId);
        }

        public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto)
        {
            if (await _db.Products.AnyAsync(p => p.Slug == dto.Slug))
                return ApiResponse<ProductDto>.Fail("A product with this slug already exists.");
            if (await _db.Products.AnyAsync(p => p.SKU == dto.SKU))
                return ApiResponse<ProductDto>.Fail("A product with this SKU already exists.");

            var product = new Product
            {
                Name = dto.Name,
                NameAr = dto.NameAr,
                Description = dto.Description,
                DescriptionAr = dto.DescriptionAr,
                Slug = dto.Slug,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                StockQuantity = dto.StockQuantity,
                SKU = dto.SKU,
                CategoryId = dto.CategoryId,
                IsFeatured = dto.IsFeatured,
                MainImageUrl = dto.MainImageUrl
            };

            foreach (var (url, i) in dto.ImageUrls.Select((u, i) => (u, i)))
                product.Images.Add(new ProductImage { Url = url, DisplayOrder = i, IsPrimary = i == 0 });

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _cache.Remove("featured_products");

            return await GetByIdAsync(product.Id, null);
        }

        public async Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto)
        {
            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return ApiResponse<ProductDto>.Fail("Product not found.");

            product.Name = dto.Name; product.NameAr = dto.NameAr; product.Description = dto.Description;
            product.DescriptionAr = dto.DescriptionAr; product.Price = dto.Price; product.DiscountPrice = dto.DiscountPrice;
            product.StockQuantity = dto.StockQuantity; product.SKU = dto.SKU; product.CategoryId = dto.CategoryId;
            product.IsFeatured = dto.IsFeatured; product.IsActive = dto.IsActive; product.MainImageUrl = dto.MainImageUrl;

            _db.ProductImages.RemoveRange(product.Images);
            foreach (var (url, i) in dto.ImageUrls.Select((u, i) => (u, i)))
                product.Images.Add(new ProductImage { Url = url, DisplayOrder = i, IsPrimary = i == 0 });

            await _db.SaveChangesAsync();
            _cache.Remove("featured_products");
            return await GetByIdAsync(id, null);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return ApiResponse<bool>.Fail("Product not found.");
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<List<ProductDto>>> GetFeaturedAsync(int count = 8)
        {
            var cacheKey = "featured_products";
            if (!_cache.TryGetValue(cacheKey, out List<ProductDto>? cached))
            {
                var products = await GetProductQuery().Where(p => p.IsActive && p.IsFeatured).Take(count).ToListAsync();
                cached = products.Select(p => MapProduct(p, false, false)).ToList();
                _cache.Set(cacheKey, cached, CacheDuration);
            }
            return ApiResponse<List<ProductDto>>.Ok(cached!);
        }

        public async Task<ApiResponse<List<ProductDto>>> GetRelatedAsync(Guid productId, int count = 4)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return ApiResponse<List<ProductDto>>.Ok(new());
            var related = await GetProductQuery()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsActive)
                .OrderByDescending(p => p.AverageRating).Take(count).ToListAsync();
            return ApiResponse<List<ProductDto>>.Ok(related.Select(p => MapProduct(p, false, false)).ToList());
        }

        public async Task<ApiResponse<bool>> UpdateStockAsync(Guid productId, int quantity)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return ApiResponse<bool>.Fail("Product not found.");
            product.StockQuantity = quantity;
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task RecordViewAsync(Guid productId, Guid userId)
        {
            var view = await _db.ProductViews.FirstOrDefaultAsync(v => v.UserId == userId && v.ProductId == productId);
            if (view == null)
                _db.ProductViews.Add(new ProductView { UserId = userId, ProductId = productId });
            else
            {
                view.ViewCount++;
                view.LastViewedAt = DateTime.UtcNow;
            }
            var product = await _db.Products.FindAsync(productId);
            if (product != null) product.ViewCount++;
            await _db.SaveChangesAsync();
        }

        private IQueryable<Product> GetProductQuery() =>
            _db.Products.Include(p => p.Category).Include(p => p.Images).Include(p => p.Variants).AsNoTracking();

        private async Task<ApiResponse<ProductDto>> BuildProductResponse(Product product, Guid? currentUserId)
        {
            bool inWishlist = false, inCart = false;
            if (currentUserId.HasValue)
            {
                inWishlist = await _db.WishlistItems.AnyAsync(w => w.UserId == currentUserId.Value && w.ProductId == product.Id);
                inCart = await _db.CartItems.AnyAsync(c => c.UserId == currentUserId.Value && c.ProductId == product.Id);
            }
            return ApiResponse<ProductDto>.Ok(MapProduct(product, inWishlist, inCart));
        }

        public static ProductDto MapProduct(Product p, bool inWishlist, bool inCart) => new()
        {
            Id = p.Id,
            Name = p.Name,
            NameAr = p.NameAr,
            Description = p.Description,
            DescriptionAr = p.DescriptionAr,
            Slug = p.Slug,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            CurrentPrice = p.CurrentPrice,
            HasDiscount = p.HasDiscount,
            DiscountPercentage = p.DiscountPercentage,
            StockQuantity = p.StockQuantity,
            SKU = p.SKU,
            MainImageUrl = p.MainImageUrl,
            IsFeatured = p.IsFeatured,
            IsActive = p.IsActive,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            SoldCount = p.SoldCount,
            Category = p.Category != null ? new CategoryDto { Id = p.Category.Id, Name = p.Category.Name, NameAr = p.Category.NameAr, Slug = p.Category.Slug } : null!,
            Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.DisplayOrder, i.IsPrimary)).ToList(),
            Variants = p.Variants.Select(v => new ProductVariantDto(v.Id, v.Size, v.Color, v.ColorHex, v.StockQuantity, v.PriceAdjustment, v.SKU)).ToList(),
            IsInWishlist = inWishlist,
            IsInCart = inCart
        };
    }
}
