using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.Product;

namespace Titan.Application.DTOs.Recommendation
{
    public record RecommendationDto(List<ProductDto> Products, string ReasonKey);
}
