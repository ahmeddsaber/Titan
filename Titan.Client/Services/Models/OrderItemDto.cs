namespace Titan.Client.Services.Models
{
    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductImageUrl { get; set; } = "";
        public string? VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }      
}
