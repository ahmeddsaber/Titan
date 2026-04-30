using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private Guid UserId
        {
            get
            {
                var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(value))
                    throw new Exception("UserId claim is missing in token");

                return Guid.Parse(value);
            }
        }
        public RecommendationsController(IRecommendationService recommendationService) { _recommendationService = recommendationService; }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int count = 8) => Ok(await _recommendationService.GetRecommendationsAsync(UserId, count));
    }

}
