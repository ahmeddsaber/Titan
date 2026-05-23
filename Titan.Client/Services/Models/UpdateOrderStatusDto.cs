using Titan.Domain.Enum;

namespace Titan.Client.Services.Models
{
    /// <summary>
    /// Concrete DTO representing order status update request on the client.
    /// Matches the backend structure exactly.
    /// </summary>
    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
    }
}
