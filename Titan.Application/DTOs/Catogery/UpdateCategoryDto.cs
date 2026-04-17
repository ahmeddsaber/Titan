using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Catogery
{
    public record UpdateCategoryDto
        (string Name, 
     string NameAr, 
     string? Description,
     string? ImageUrl
     , bool IsActive,
     int DisplayOrder);
}
