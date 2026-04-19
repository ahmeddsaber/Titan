using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.DTOs.Review;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private Guid? UserId => User.Identity?.IsAuthenticated == true ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : null;
        public ReviewsController(IReviewService reviewService) { _reviewService = reviewService; }

        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetProductReviews(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            Ok(await _reviewService.GetProductReviewsAsync(productId, page, pageSize));

        [HttpPost, Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var result = await _reviewService.CreateReviewAsync(UserId!.Value, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}"), Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _reviewService.DeleteReviewAsync(id, UserId!.Value, User.IsInRole("Admin"));
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/helpful")]
        public async Task<IActionResult> MarkHelpful(Guid id) => Ok(await _reviewService.MarkHelpfulAsync(id));
    }
}
