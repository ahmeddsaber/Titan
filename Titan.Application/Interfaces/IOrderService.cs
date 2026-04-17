using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Order;
using Titan.Application.DTOs.Pagination;
using Titan.Domain.Enum;

namespace Titan.Application.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDto>> CreateFromCartAsync(Guid userId, CreateOrderDto dto);
        Task<ApiResponse<PagedResult<OrderDto>>> GetUserOrdersAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<OrderDto>> GetByIdAsync(Guid orderId, Guid? userId = null);
        Task<ApiResponse<PagedResult<OrderDto>>> GetAllAsync(int page, int pageSize, OrderStatus? status);
        Task<ApiResponse<OrderDto>> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, Guid adminId);
        Task<ApiResponse<bool>> CancelOrderAsync(Guid orderId, Guid userId);
    }
}
