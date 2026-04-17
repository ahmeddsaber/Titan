using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Notification;
using Titan.Domain.Enum;

namespace Titan.Application.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int count = 20);
        Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId);
        Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid userId);
        Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? actionUrl = null);
        Task SendOrderNotificationAsync(Guid userId, string orderNumber, OrderStatus status);
    }
}
