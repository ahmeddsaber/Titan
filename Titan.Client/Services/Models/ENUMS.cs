using System.Text.Json.Serialization;

namespace Titan.Client.Services.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus
    {
        Pending = 0, Confirmed = 1, Processing = 2,
        Shipped = 3, OutForDelivery = 4, Delivered = 5,
        Cancelled = 6, Refunded = 7, Failed = 8
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DiscountType { Percentage = 0, FixedAmount = 1 }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType { OrderUpdate = 0, Promotion = 1, System = 2, Welcome = 3, Review = 4 }

}
