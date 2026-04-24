namespace Titan.Client.Services.Models
{
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }
        public List<RevenuePoint> RevenueChart { get; set; } = new();
        public List<TopProductStat> TopProducts { get; set; } = new();
        public List<RecentOrderStat> RecentOrders { get; set; } = new();
    }
    public class RevenuePoint { public string Label { get; set; } = ""; public decimal Revenue { get; set; } public int Orders { get; set; } }
    public class TopProductStat { public Guid Id { get; set; } public string Name { get; set; } = ""; public string ImageUrl { get; set; } = ""; public int SoldCount { get; set; } public decimal Revenue { get; set; } }
    public class RecentOrderStat { public Guid Id { get; set; } public string OrderNumber { get; set; } = ""; public string CustomerName { get; set; } = ""; public decimal TotalAmount { get; set; } public OrderStatus Status { get; set; } public DateTime CreatedAt { get; set; } }

}
