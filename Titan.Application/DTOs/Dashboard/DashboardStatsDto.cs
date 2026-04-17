using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Dashboard
{
    public record DashboardStatsDto
    {
        public int TotalUsers { get; init; }
        public int TotalProducts { get; init; }
        public int TotalOrders { get; init; }
        public decimal TotalRevenue { get; init; }
        public int PendingOrders { get; init; }
        public int LowStockProducts { get; init; }
        public List<RevenueChartDto> RevenueChart { get; init; } = new();
        public List<TopProductDto> TopProducts { get; init; } = new();
        public List<RecentOrderDto> RecentOrders { get; init; } = new();
    }
}
