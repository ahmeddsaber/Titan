using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Dashboard;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.User;

namespace Titan.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, string? search);
        Task<ApiResponse<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<ApiResponse<string>> UpdateProfileImageAsync(Guid userId, Stream imageStream, string fileName);
        Task<ApiResponse<bool>> BanUserAsync(Guid userId, string reason);
        Task<ApiResponse<bool>> UnbanUserAsync(Guid userId);
        Task<ApiResponse<bool>> DeleteUserAsync(Guid userId);
        Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync();
    }
}
