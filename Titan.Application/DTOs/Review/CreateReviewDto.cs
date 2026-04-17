using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Review
{
    public record CreateReviewDto(Guid ProductId, int Rating, string Comment);
}
