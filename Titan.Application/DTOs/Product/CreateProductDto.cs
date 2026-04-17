using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Product
{
    public record CreateProductDto
    {
        public string Name { get; init; } = string.Empty;
        public string NameAr { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string DescriptionAr { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public decimal? DiscountPrice { get; init; }
        public int StockQuantity { get; init; }
        public string SKU { get; init; } = string.Empty;
        public Guid CategoryId { get; init; }
        public bool IsFeatured { get; init; }
        public string MainImageUrl { get; init; } = string.Empty;
        public List<string> ImageUrls { get; init; } = new();
    }
}
