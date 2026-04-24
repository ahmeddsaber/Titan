namespace Titan.Client.Services.Models
{
    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public string? AppliedCouponCode { get; set; }
    }
}
