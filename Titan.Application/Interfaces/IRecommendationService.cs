using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Recommendation;

namespace Titan.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<ApiResponse<RecommendationDto>> GetRecommendationsAsync(Guid userId, int count = 8);
    }
}
