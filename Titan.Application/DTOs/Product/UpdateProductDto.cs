using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Product
{
    public record UpdateProductDto : CreateProductDto
    {
        public bool IsActive { get; init; }
    }
}
