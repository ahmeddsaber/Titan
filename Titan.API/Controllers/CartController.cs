using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.DTOs.Cart;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService) { _cartService = cartService; }

        // FIX #3: Safe UserId extraction — prevents 500 when claim is missing
        private bool TryGetUserId(out Guid userId)
            => Guid.TryParse(User.Identity?.Name, out userId);

        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] string? coupon)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cartService.GetCartAsync(userId, coupon));
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cartService.AddToCartAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCartItemDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cartService.UpdateQuantityAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{itemId:guid}")]
        public async Task<IActionResult> Remove(Guid itemId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cartService.RemoveFromCartAsync(userId, itemId));
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cartService.ClearCartAsync(userId));
        }

        [HttpPost("validate-coupon")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ApplyCouponDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var cart = await _cartService.GetCartAsync(userId);
            var result = await _cartService.ValidateCouponAsync(dto.CouponCode, cart.Data?.SubTotal ?? 0);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
