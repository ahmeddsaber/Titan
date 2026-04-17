using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Cart
{
    public record CartItemDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string ProductSlug { get; init; } = string.Empty;
        public string ProductImageUrl { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
        public decimal TotalPrice { get; init; }
        public int StockQuantity { get; init; }
        public Guid? VariantId { get; init; }
        public string? VariantInfo { get; init; }
    }
}
