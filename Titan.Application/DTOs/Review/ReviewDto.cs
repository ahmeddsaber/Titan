using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Review
{
    public record ReviewDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string? UserImageUrl { get; init; }
        public int Rating { get; init; }
        public string Comment { get; init; } = string.Empty;
        public bool IsVerifiedPurchase { get; init; }
        public int HelpfulCount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
