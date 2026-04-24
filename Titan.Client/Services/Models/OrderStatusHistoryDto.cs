namespace Titan.Client.Services.Models
{
    public class OrderStatusHistoryDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
