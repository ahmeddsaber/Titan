using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.Review;

namespace Titan.Application.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResponse<PagedResult<ReviewDto>>> GetProductReviewsAsync(Guid productId, int page, int pageSize);
        Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto);
        Task<ApiResponse<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin);
        Task<ApiResponse<bool>> MarkHelpfulAsync(Guid reviewId);
    }
}
