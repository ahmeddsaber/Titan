namespace Titan.Client.Services.Models
{
    public class CouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public decimal? MaximumDiscountAmount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public int? RemainingUses { get; set; }
    }
}
