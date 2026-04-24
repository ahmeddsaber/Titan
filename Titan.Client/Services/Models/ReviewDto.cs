namespace Titan.Client.Services.Models
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string? UserImageUrl { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public bool IsVerifiedPurchase { get; set; }
        public int HelpfulCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
