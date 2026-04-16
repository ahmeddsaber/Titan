using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Domain.Enum;
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7,
    Failed = 8
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum NotificationType
{
    OrderUpdate = 0,
    Promotion = 1,
    System = 2,
    Welcome = 3,
    Review = 4
}
