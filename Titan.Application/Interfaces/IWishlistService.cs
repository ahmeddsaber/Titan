using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Product;

namespace Titan.Application.Interfaces
{
    public interface IWishlistService
    {
        Task<ApiResponse<List<ProductDto>>> GetWishlistAsync(Guid userId);
        Task<ApiResponse<bool>> AddToWishlistAsync(Guid userId, Guid productId);
        Task<ApiResponse<bool>> RemoveFromWishlistAsync(Guid userId, Guid productId);
        Task<ApiResponse<bool>> IsInWishlistAsync(Guid userId, Guid productId);
    }
}
