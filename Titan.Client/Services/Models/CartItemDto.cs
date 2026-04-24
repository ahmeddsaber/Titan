namespace Titan.Client.Services.Models
{
    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSlug { get; set; } = "";
        public string ProductImageUrl { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public int StockQuantity { get; set; }
        public Guid? VariantId { get; set; }
        public string? VariantInfo { get; set; }
    }
}
