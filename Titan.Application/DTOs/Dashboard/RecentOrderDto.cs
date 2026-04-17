using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Dashboard
{
    public record RecentOrderDto(Guid Id, string OrderNumber, string CustomerName, decimal TotalAmount, OrderStatus Status, DateTime CreatedAt);
}
