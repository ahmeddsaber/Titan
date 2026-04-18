using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.Review;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Domain.Enum;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _db;
        public ReviewService(ApplicationDbContext db) { _db = db; }

        public async Task<ApiResponse<PagedResult<ReviewDto>>> GetProductReviewsAsync(Guid productId, int page, int pageSize)
        {
            var query = _db.Reviews.Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt);
            var total = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<ReviewDto>>.Ok(new PagedResult<ReviewDto>
            {
                Items = reviews.Select(MapReview).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto)
        {
            if (await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == dto.ProductId))
                return ApiResponse<ReviewDto>.Fail("You have already reviewed this product.");
            if (dto.Rating < 1 || dto.Rating > 5) return ApiResponse<ReviewDto>.Fail("Rating must be between 1 and 5.");
            var isVerified = await _db.OrderItems.AnyAsync(oi => oi.ProductId == dto.ProductId && oi.Order.UserId == userId && oi.Order.Status == OrderStatus.Delivered);
            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                IsVerifiedPurchase = isVerified
            };
            _db.Reviews.Add(review);
            await RecalculateRatingAsync(dto.ProductId);
            await _db.SaveChangesAsync();
            var user = await _db.Users.FindAsync(userId);
            return ApiResponse<ReviewDto>.Ok(MapReview(review) with { UserName = user?.FullName ?? "User" });
        }

        public async Task<ApiResponse<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin)
        {
            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && (isAdmin || r.UserId == userId));
            if (review == null) return ApiResponse<bool>.Fail("Review not found.");
            _db.Reviews.Remove(review);
            await RecalculateRatingAsync(review.ProductId);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<bool>> MarkHelpfulAsync(Guid reviewId)
        {
            var review = await _db.Reviews.FindAsync(reviewId);
            if (review == null) return ApiResponse<bool>.Fail("Not found.");
            review.HelpfulCount++;
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        private async Task RecalculateRatingAsync(Guid productId)
        {
            var reviews = await _db.Reviews.Where(r => r.ProductId == productId && r.IsApproved).ToListAsync();
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return;
            product.ReviewCount = reviews.Count;
            product.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }

        private static ReviewDto MapReview(Review r) => new()
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = r.User?.FullName ?? "User",
            UserImageUrl = r.User?.ProfileImageUrl,
            Rating = r.Rating,
            Comment = r.Comment,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            HelpfulCount = r.HelpfulCount,
            CreatedAt = r.CreatedAt
        };
    }
}
