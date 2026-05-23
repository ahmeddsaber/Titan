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
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        public WishlistController(IWishlistService wishlistService) { _wishlistService = wishlistService; }

        // FIX #3: Safe UserId extraction
        private bool TryGetUserId(out Guid userId)
            => Guid.TryParse(User.Identity?.Name, out userId);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _wishlistService.GetWishlistAsync(userId));
        }

        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> Add(Guid productId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _wishlistService.AddToWishlistAsync(userId, productId));
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> Remove(Guid productId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _wishlistService.RemoveFromWishlistAsync(userId, productId));
        }
    }
}
