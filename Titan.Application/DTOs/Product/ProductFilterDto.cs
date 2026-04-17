using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Product;
public record ProductFilterDto
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? HasDiscount { get; init; }
    public string? SortBy { get; init; } = "newest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
