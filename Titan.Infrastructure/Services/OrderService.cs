using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Order;
using Titan.Application.DTOs.Pagination;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Domain.Enum;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Titan.Infrastructure.Hubs.TitanHub> _hubContext;

        public OrderService(
            ApplicationDbContext db, 
            INotificationService notificationService,
            Microsoft.AspNetCore.SignalR.IHubContext<Titan.Infrastructure.Hubs.TitanHub> hubContext)
        {
            _db = db;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse<OrderDto>> CreateFromCartAsync(Guid userId, CreateOrderDto dto)
        {
            var cartItems = await _db.CartItems.Include(c => c.Product).Include(c => c.Variant)
                .Where(c => c.UserId == userId).ToListAsync();

            if (!cartItems.Any()) return ApiResponse<OrderDto>.Fail("Your cart is empty.");

            // Stock check
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                    return ApiResponse<OrderDto>.Fail($"Insufficient stock for {item.Product.Name}.");
            }

            var subTotal = cartItems.Sum(i => i.Product.CurrentPrice * i.Quantity);
            decimal discount = 0;
            Coupon? coupon = null;

            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == dto.CouponCode.ToUpper() && c.IsActive);
                if (coupon != null)
                {
                    discount = coupon.DiscountType == DiscountType.Percentage
                        ? subTotal * (coupon.DiscountValue / 100)
                        : coupon.DiscountValue;
                    if (coupon.MaximumDiscountAmount.HasValue)
                        discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
                    coupon.UsageCount++;
                }
            }

            var shipping = subTotal > 500 ? 0 : 50;
            var orderNumber = $"TT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                Status = OrderStatus.Pending,
                SubTotal = subTotal,
                DiscountAmount = discount,
                ShippingCost = shipping,
                TotalAmount = subTotal - discount + shipping,
                CouponCode = dto.CouponCode?.ToUpper(),
                CouponId = coupon?.Id,
                ShippingFullName = dto.ShippingFullName,
                ShippingPhone = dto.ShippingPhone,
                ShippingAddress = dto.ShippingAddress,
                ShippingCity = dto.ShippingCity,
                ShippingCountry = dto.ShippingCountry,
                ShippingPostalCode = dto.ShippingPostalCode,
                Notes = dto.Notes,
                EstimatedDelivery = DateTime.UtcNow.AddDays(7)
            };

            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductImageUrl = item.Product.MainImageUrl,
                    VariantInfo = item.Variant != null ? $"{item.Variant.Size} / {item.Variant.Color}" : null,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.CurrentPrice,
                    TotalPrice = item.Product.CurrentPrice * item.Quantity
                });
                item.Product.StockQuantity -= item.Quantity;
                item.Product.SoldCount += item.Quantity;
            }

            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Order placed successfully." });
            _db.Orders.Add(order);
            _db.CartItems.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            await _notificationService.SendOrderNotificationAsync(userId, orderNumber, OrderStatus.Pending);
            return ApiResponse<OrderDto>.Ok(await GetOrderDtoAsync(order.Id), "Order placed successfully!");
        }

        public async Task<ApiResponse<PagedResult<OrderDto>>> GetUserOrdersAsync(Guid userId, int page, int pageSize)
        {
            var query = _db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt);
            var total = await query.CountAsync();
            var orders = await query.Include(o => o.Items).ThenInclude(i => i.Product)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>
            {
                Items = orders.Select(MapOrder).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<OrderDto>> GetByIdAsync(Guid orderId, Guid? userId = null)
        {
            var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId && (userId == null || o.UserId == userId));
            if (order == null) return ApiResponse<OrderDto>.Fail("Order not found.");
            return ApiResponse<OrderDto>.Ok(MapOrder(order));
        }

        public async Task<ApiResponse<PagedResult<OrderDto>>> GetAllAsync(int page, int pageSize, OrderStatus? status)
        {
            var query = _db.Orders.Include(o => o.Items).Include(o => o.User).AsQueryable();
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            query = query.OrderByDescending(o => o.CreatedAt);
            var total = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>
            {
                Items = orders.Select(MapOrder).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<OrderDto>> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, Guid adminId)
        {
            if (dto == null)
                return ApiResponse<OrderDto>.Fail("Invalid update request DTO.");

            if (!Enum.IsDefined(typeof(OrderStatus), dto.Status))
                return ApiResponse<OrderDto>.Fail("?????? ???????? ??? ?????.");
            

            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return ApiResponse<OrderDto>.Fail("Order not found.");

            // Stock restoration if transitioning to Cancelled from another state
            if (dto.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                RestoreStock(order);
            }

            order.Status    = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == OrderStatus.Delivered)
                order.DeliveredAt = DateTime.UtcNow;

            // Ensure collection is not null
            order.StatusHistory ??= new List<OrderStatusHistory>();

            // Explicitly set OrderId and Order to prevent EF tracking/validation conflicts
            var historyEntry = new OrderStatusHistory
            {
                OrderId          = order.Id,
                Order            = order,
                Status           = dto.Status,
                Note             = dto.Note,
                ChangedByUserId  = adminId,
                CreatedAt        = DateTime.UtcNow
            };
            order.StatusHistory.Add(historyEntry);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return ApiResponse<OrderDto>.Fail($"Failed to update order status: {ex.Message}");
            }

            // Re-query with AsNoTracking so the returned DTO reflects the DB state
            // (not the in-memory tracked object which may have stale defaults)
            Order? fresh = null;
            try
            {
                fresh = await _db.Orders
                    .Include(o => o.Items)
                    .Include(o => o.StatusHistory)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception)
            {
                // Fall back gracefully to the tracked entity if AsNoTracking query fails
            }

            var orderDto = MapOrder(fresh ?? order);

            // 1. Send database-backed notification to the customer (awaited inside request scope)
            try
            {
                await _notificationService.SendOrderNotificationAsync(order.UserId, order.OrderNumber, dto.Status);
            }
            catch (Exception)
            {
                // Safely catch notification DB insert/SignalR exceptions so it never blocks the request or rolls back order updates
            }

            // 2. Broadcast real-time order update via SignalR to the customer and all connected admins
            try
            {
                // Send to customer
                await _hubContext.Clients.User(order.UserId.ToString())
                    .SendAsync("OrderStatusUpdated", orderDto);

                // Send to all connected Admins to update their dashboard in real-time
                await _hubContext.Clients.Group("admins")
                    .SendAsync("OrderStatusUpdated", orderDto);
            }
            catch (Exception)
            {
                // Safely ignore/log SignalR broadcast failures
            }

            return ApiResponse<OrderDto>.Ok(orderDto);
        }

        public async Task<ApiResponse<bool>> CancelOrderAsync(Guid orderId, Guid userId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return ApiResponse<bool>.Fail("Order not found.");

            if (!CanBeCancelled(order.Status))
                return ApiResponse<bool>.Fail("Order cannot be cancelled at this stage.");

            order.StatusHistory ??= new List<OrderStatusHistory>();

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            RestoreStock(order);

            // Explicitly set OrderId and Order to prevent EF tracking/validation conflicts
            var historyEntry = new OrderStatusHistory
            {
                OrderId         = order.Id,
                Order           = order,
                Status          = OrderStatus.Cancelled,
                Note            = "Cancelled by customer",
                ChangedByUserId = userId,
                CreatedAt       = DateTime.UtcNow
            };
            order.StatusHistory.Add(historyEntry);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail($"Failed to cancel order: {ex.Message}");
            }

            var orderDto = MapOrder(order);

            // 1. Send database-backed notification to the customer
            try
            {
                await _notificationService.SendOrderNotificationAsync(order.UserId, order.OrderNumber, OrderStatus.Cancelled);
            }
            catch (Exception)
            {
                // Safely catch notification exceptions
            }

            // 2. Broadcast real-time order update via SignalR to the customer and all connected admins
            try
            {
                // Send to customer
                await _hubContext.Clients.User(order.UserId.ToString())
                    .SendAsync("OrderStatusUpdated", orderDto);

                // Send to all connected Admins to update their dashboard in real-time
                await _hubContext.Clients.Group("admins")
                    .SendAsync("OrderStatusUpdated", orderDto);
            }
            catch (Exception)
            {
                // Safely ignore SignalR broadcast failures
            }

            return ApiResponse<bool>.Ok(true, "Order cancelled successfully.");
        }
        private static bool CanBeCancelled(OrderStatus status)
        {
            return status is OrderStatus.Pending or OrderStatus.Confirmed;
        }
        private static void RestoreStock(Order order)
        {
            if (order == null || order.Items == null) return;

            foreach (var item in order.Items)
            {
                if (item == null || item.Product == null) continue;

                item.Product.StockQuantity += item.Quantity;
                item.Product.SoldCount = Math.Max(0, item.Product.SoldCount - item.Quantity);
            }
        }
        private async Task<OrderDto> GetOrderDtoAsync(Guid orderId)
        {
            var order = await _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).FirstOrDefaultAsync(o => o.Id == orderId);
            return MapOrder(order!);
        }

        private static OrderDto MapOrder(Order o) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            StatusLabel = o.Status.ToString(),
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            ShippingCost = o.ShippingCost,
            TotalAmount = o.TotalAmount,
            CouponCode = o.CouponCode,
            ShippingFullName = o.ShippingFullName,
            ShippingPhone = o.ShippingPhone,
            ShippingAddress = o.ShippingAddress,
            ShippingCity = o.ShippingCity,
            ShippingCountry = o.ShippingCountry,
            Notes = o.Notes,
            EstimatedDelivery = o.EstimatedDelivery,
            DeliveredAt = o.DeliveredAt,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.ProductImageUrl, i.VariantInfo, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            StatusHistory = o.StatusHistory.OrderByDescending(h => h.CreatedAt).Select(h => new OrderStatusHistoryDto(h.Status, h.Status.ToString(), h.Note, h.CreatedAt)).ToList()
        };
    }
}
