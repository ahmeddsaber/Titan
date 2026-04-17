using Microsoft.EntityFrameworkCore;
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

        public OrderService(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
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
            var order = await _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return ApiResponse<OrderDto>.Fail("Order not found.");
            order.Status = dto.Status;
            if (dto.Status == OrderStatus.Delivered) order.DeliveredAt = DateTime.UtcNow;
            order.StatusHistory.Add(new OrderStatusHistory { Status = dto.Status, Note = dto.Note, ChangedByUserId = adminId });
            await _db.SaveChangesAsync();
            await _notificationService.SendOrderNotificationAsync(order.UserId, order.OrderNumber, dto.Status);
            return ApiResponse<OrderDto>.Ok(MapOrder(order));
        }

        public async Task<ApiResponse<bool>> CancelOrderAsync(Guid orderId, Guid userId)
        {
            var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) return ApiResponse<bool>.Fail("Order not found.");
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                return ApiResponse<bool>.Fail("Order cannot be cancelled at this stage.");

            order.Status = OrderStatus.Cancelled;
            foreach (var item in order.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null) { product.StockQuantity += item.Quantity; product.SoldCount -= item.Quantity; }
            }
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, Note = "Cancelled by customer.", ChangedByUserId = userId });
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Order cancelled.");
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
