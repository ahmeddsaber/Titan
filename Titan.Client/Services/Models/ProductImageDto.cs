namespace Titan.Client.Services.Models
{
    public class ProductImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = "";
        public string AltText { get; set; } = "";
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
