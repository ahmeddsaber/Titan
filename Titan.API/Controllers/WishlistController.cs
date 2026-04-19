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
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        public WishlistController(IWishlistService wishlistService) { _wishlistService = wishlistService; }

        [HttpGet] public async Task<IActionResult> Get() => Ok(await _wishlistService.GetWishlistAsync(UserId));
        [HttpPost("{productId:guid}")] public async Task<IActionResult> Add(Guid productId) => Ok(await _wishlistService.AddToWishlistAsync(UserId, productId));
        [HttpDelete("{productId:guid}")] public async Task<IActionResult> Remove(Guid productId) => Ok(await _wishlistService.RemoveFromWishlistAsync(UserId, productId));
    }
}
