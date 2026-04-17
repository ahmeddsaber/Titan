using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.Product;

namespace Titan.Application.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResult<ProductDto>>> GetAllAsync(ProductFilterDto filter, Guid? currentUserId);
        Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<ApiResponse<ProductDto>> GetBySlugAsync(string slug, Guid? currentUserId);
        Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto);
        Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
        Task<ApiResponse<List<ProductDto>>> GetFeaturedAsync(int count = 8);
        Task<ApiResponse<List<ProductDto>>> GetRelatedAsync(Guid productId, int count = 4);
        Task<ApiResponse<bool>> UpdateStockAsync(Guid productId, int quantity);
        Task RecordViewAsync(Guid productId, Guid userId);
    }
}
