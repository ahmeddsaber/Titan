using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Titan.Application.DTOs.Coupon;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;
        public CouponsController(ICouponService couponService) { _couponService = couponService; }

        [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20) => Ok(await _couponService.GetAllAsync(page, pageSize));
        [HttpPost] public async Task<IActionResult> Create([FromBody] CreateCouponDto dto) => Ok(await _couponService.CreateAsync(dto));
        [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) => Ok(await _couponService.DeleteAsync(id));
        [HttpPost("{id:guid}/toggle")] public async Task<IActionResult> Toggle(Guid id) => Ok(await _couponService.ToggleActiveAsync(id));
    }
}
