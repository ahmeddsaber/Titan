using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Product
{
    public record ProductVariantDto(Guid Id, string Size, string Color, string ColorHex, int StockQuantity, decimal? PriceAdjustment, string SKU);

}
