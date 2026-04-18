using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Dashboard;
using Titan.Application.DTOs.Pagination;
using Titan.Application.DTOs.User;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Domain.Enum;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services;
public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    public UserService(ApplicationDbContext db) { _db = db; }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? ApiResponse<UserDto>.Fail("User not found.") : ApiResponse<UserDto>.Ok(MapUser(user));
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search));
        var total = await query.CountAsync();
        var users = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
        return ApiResponse<PagedResult<UserDto>>.Ok(new PagedResult<UserDto>
        {
            Items = users.Select(MapUser).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<UserDto>.Fail("User not found.");
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.PreferredLanguage = dto.PreferredLanguage;
        await _db.SaveChangesAsync();
        return ApiResponse<UserDto>.Ok(MapUser(user));
    }

    public async Task<ApiResponse<string>> UpdateProfileImageAsync(Guid userId, Stream imageStream, string fileName)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<string>.Fail("User not found.");
        var ext = Path.GetExtension(fileName);
        var imageName = $"profiles/{userId}{ext}";
        // In production: upload to Azure Blob / AWS S3
        var url = $"/images/{imageName}";
        user.ProfileImageUrl = url;
        await _db.SaveChangesAsync();
        return ApiResponse<string>.Ok(url, "Profile image updated.");
    }

    public async Task<ApiResponse<bool>> BanUserAsync(Guid userId, string reason)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<bool>.Fail("User not found.");
        if (user.Role == "Admin") return ApiResponse<bool>.Fail("Cannot ban an admin.");
        user.IsBanned = true;
        user.BanReason = reason;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "User banned.");
    }

    public async Task<ApiResponse<bool>> UnbanUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<bool>.Fail("User not found.");
        user.IsBanned = false;
        user.BanReason = null;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "User unbanned.");
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResponse<bool>.Fail("User not found.");
        if (user.Role == "Admin") return ApiResponse<bool>.Fail("Cannot delete an admin.");
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalProducts = await _db.Products.CountAsync();
        var totalOrders = await _db.Orders.CountAsync();
        var totalRevenue = await _db.Orders.Where(o => o.Status == OrderStatus.Delivered).SumAsync(o => o.TotalAmount);
        var pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);
        var lowStock = await _db.Products.CountAsync(p => p.StockQuantity < 10);

        // Revenue chart (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var revenueData = await _db.Orders
            .Where(o => o.CreatedAt >= sevenDaysAgo)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
            .OrderBy(x => x.Date).ToListAsync();

        var revenueChart = revenueData.Select(r => new RevenueChartDto(r.Date.ToString("MMM dd"), r.Revenue, r.Orders)).ToList();

        // Top products
        var topProducts = await _db.Products
            .OrderByDescending(p => p.SoldCount).Take(5).AsNoTracking()
            .Select(p => new TopProductDto(p.Id, p.Name, p.MainImageUrl, p.SoldCount, p.SoldCount * p.Price)).ToListAsync();

        // Recent orders
        var recentOrders = await _db.Orders.Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt).Take(10).AsNoTracking()
            .Select(o => new RecentOrderDto(o.Id, o.OrderNumber, o.User.FullName, o.TotalAmount, o.Status, o.CreatedAt)).ToListAsync();

        return ApiResponse<DashboardStatsDto>.Ok(new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            PendingOrders = pendingOrders,
            LowStockProducts = lowStock,
            RevenueChart = revenueChart,
            TopProducts = topProducts,
            RecentOrders = recentOrders
        });
    }

    private static UserDto MapUser(User u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role,
        ProfileImageUrl = u.ProfileImageUrl,
        IsActive = u.IsActive,
        IsBanned = u.IsBanned,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
        PreferredLanguage = u.PreferredLanguage
    };
}


