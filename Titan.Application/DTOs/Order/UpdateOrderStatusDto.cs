using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Order
{
    public record UpdateOrderStatusDto(OrderStatus Status, string? Note);
}
