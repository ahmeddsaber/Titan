using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Order
{
    public record OrderItemDto(Guid ProductId, string ProductName, string ProductImageUrl, string? VariantInfo, int Quantity, decimal UnitPrice, decimal TotalPrice);

}
