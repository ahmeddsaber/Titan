//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Titan.Application.DTOs.API_Response;
//using Titan.Application.DTOs.Notification;
//using Titan.Application.Interfaces;
//using Titan.Domain.Entities;
//using Titan.Domain.Enum;
//using Titan.Infrastructure.Data;

//namespace Titan.Infrastructure.Services
//{
//    public class NotificationService : INotificationService
//    {
//        private readonly ApplicationDbContext _db;
//        private IHubContext<TitanHub>? _hubContext;

//        public NotificationService(ApplicationDbContext db) { _db = db; }

//        public void SetHubContext(IHubContext<TitanHub> hub) => _hubContext = hub;

//        public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int count = 20)
//        {
//            var notifications = await _db.Notifications
//                .Where(n => n.UserId == userId)
//                .OrderByDescending(n => n.CreatedAt)
//                .Take(count).AsNoTracking().ToListAsync();
//            return ApiResponse<List<NotificationDto>>.Ok(notifications.Select(MapNotification).ToList());
//        }

//        public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId)
//        {
//            var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
//            return ApiResponse<int>.Ok(count);
//        }

//        public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
//        {
//            var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
//            if (n == null) return ApiResponse<bool>.Fail("Not found.");
//            n.IsRead = true; n.ReadAt = DateTime.UtcNow;
//            await _db.SaveChangesAsync();
//            return ApiResponse<bool>.Ok(true);
//        }

//        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid userId)
//        {
//            var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
//            foreach (var n in notifications) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; }
//            await _db.SaveChangesAsync();
//            return ApiResponse<bool>.Ok(true);
//        }

//        public async Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? actionUrl = null)
//        {
//            var notification = new Notification
//            {
//                UserId = userId,
//                Title = title,
//                Message = message,
//                Type = type,
//                ActionUrl = actionUrl,
//                IconClass = type switch
//                {
//                    NotificationType.OrderUpdate => "fas fa-box",
//                    NotificationType.Promotion => "fas fa-tag",
//                    NotificationType.Welcome => "fas fa-star",
//                    _ => "fas fa-bell"
//                }
//            };
//            _db.Notifications.Add(notification);
//            await _db.SaveChangesAsync();

//            if (_hubContext != null)
//            {
//                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", MapNotification(notification));
//            }
//        }

//        public async Task SendOrderNotificationAsync(Guid userId, string orderNumber, OrderStatus status)
//        {
//            var message = status switch
//            {
//                OrderStatus.Pending => $"Your order #{orderNumber} has been placed successfully!",
//                OrderStatus.Confirmed => $"Your order #{orderNumber} has been confirmed.",
//                OrderStatus.Processing => $"Your order #{orderNumber} is being processed.",
//                OrderStatus.Shipped => $"Your order #{orderNumber} has been shipped!",
//                OrderStatus.OutForDelivery => $"Your order #{orderNumber} is out for delivery!",
//                OrderStatus.Delivered => $"Your order #{orderNumber} has been delivered! Enjoy your TITAN gear.",
//                OrderStatus.Cancelled => $"Your order #{orderNumber} has been cancelled.",
//                _ => $"Order #{orderNumber} status updated."
//            };
//            await SendNotificationAsync(userId, "Order Update 📦", message, NotificationType.OrderUpdate, $"/orders/{orderNumber}");
//        }

//        private static NotificationDto MapNotification(Notification n) => new()
//        {
//            Id = n.Id,
//            Title = n.Title,
//            Message = n.Message,
//            Type = n.Type,
//            IsRead = n.IsRead,
//            ActionUrl = n.ActionUrl,
//            IconClass = n.IconClass,
//            CreatedAt = n.CreatedAt
//        };
//    }
//}
