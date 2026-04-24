namespace Titan.Client.Services.Models
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
        public string? IconClass { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
