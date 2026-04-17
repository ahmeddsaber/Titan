using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Catogery;

namespace Titan.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<List<CategoryDto>>> GetAllAsync();
        Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto);
        Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
