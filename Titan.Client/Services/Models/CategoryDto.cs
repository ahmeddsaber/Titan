namespace Titan.Client.Services.Models
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string NameAr { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryDto> Children { get; set; } = new();
    }
}
