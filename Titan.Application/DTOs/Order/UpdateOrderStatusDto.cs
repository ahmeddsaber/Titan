using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Order;

/// <summary>
/// DTO sent by the admin client to update an order's status.
/// Plain class (not positional record) so System.Text.Json can bind
/// the JSON body properties by name without a [JsonConstructor].
/// </summary>
public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
    public string?     Note   { get; set; }
}
