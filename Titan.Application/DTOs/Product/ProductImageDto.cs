using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Product
{
    public record ProductImageDto(Guid Id, string Url, string AltText, int DisplayOrder, bool IsPrimary);
}
