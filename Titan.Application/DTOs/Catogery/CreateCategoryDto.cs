using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Catogery
{

    public record CreateCategoryDto(string Name, 
        string NameAr, string Slug, string? Description,
        string? ImageUrl,
        Guid? ParentId, int DisplayOrder);
}
