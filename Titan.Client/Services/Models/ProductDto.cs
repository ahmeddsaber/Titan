namespace Titan.Client.Services.Models
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string NameAr { get; set; } = "";
        public string Description { get; set; } = "";
        public string DescriptionAr { get; set; } = "";
        public string Slug { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool HasDiscount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; } = "";
        public string MainImageUrl { get; set; } = "";
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int SoldCount { get; set; }
        public CategoryDto? Category { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public bool IsInWishlist { get; set; }
        public bool IsInCart { get; set; }
    }
}
