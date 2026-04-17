using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Auth;

namespace Titan.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }
}
