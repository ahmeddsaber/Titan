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
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        public CartController(ICartService cartService) { _cartService = cartService; }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] string? coupon) =>
            Ok(await _cartService.GetCartAsync(UserId, coupon));

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
        {
            var result = await _cartService.AddToCartAsync(UserId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCartItemDto dto)
        {
            var result = await _cartService.UpdateQuantityAsync(UserId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{itemId:guid}")]
        public async Task<IActionResult> Remove(Guid itemId) =>
            Ok(await _cartService.RemoveFromCartAsync(UserId, itemId));

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear() => Ok(await _cartService.ClearCartAsync(UserId));

        [HttpPost("validate-coupon")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ApplyCouponDto dto)
        {
            var cart = await _cartService.GetCartAsync(UserId);
            var result = await _cartService.ValidateCouponAsync(dto.CouponCode, cart.Data?.SubTotal ?? 0);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }

}
