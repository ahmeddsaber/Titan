using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.Catogery;

namespace Titan.Application.DTOs.Product;
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DescriptionAr { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? DiscountPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public bool HasDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public int StockQuantity { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string MainImageUrl { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int SoldCount { get; init; }
    public CategoryDto Category { get; init; } = null!;
    public List<ProductImageDto> Images { get; init; } = new();
    public List<ProductVariantDto> Variants { get; init; } = new();
    public bool IsInWishlist { get; init; }
    public bool IsInCart { get; init; }
}
