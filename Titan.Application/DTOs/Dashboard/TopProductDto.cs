using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Dashboard
{
    public record TopProductDto(Guid Id, string Name, string ImageUrl, int SoldCount, decimal Revenue)
}
