using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Enum;

namespace Titan.Application.DTOs.Notification
{
    public record NotificationDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public NotificationType Type { get; init; }
        public bool IsRead { get; init; }
        public string? ActionUrl { get; init; }
        public string? IconClass { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
