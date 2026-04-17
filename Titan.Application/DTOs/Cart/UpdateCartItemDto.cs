using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Cart
{
    public record UpdateCartItemDto(Guid CartItemId, int Quantity);
}
