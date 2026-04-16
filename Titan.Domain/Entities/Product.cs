using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = "TITAN";
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public double AverageRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public int ViewCount { get; set; } = 0;
    public int SoldCount { get; set; } = 0;

    // Images
    public string MainImageUrl { get; set; } = string.Empty;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    // Variants
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    // Category
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Navigation
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ProductView> ProductViews { get; set; } = new List<ProductView>();

    public decimal CurrentPrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 ? DiscountPrice.Value : Price;
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice.Value < Price;
    public decimal DiscountPercentage => HasDiscount ? Math.Round((Price - DiscountPrice!.Value) / Price * 100, 0) : 0;
}