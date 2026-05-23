using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Order;
using Titan.Application.Interfaces;
using Titan.Domain.Enum;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrdersController(IOrderService orderService) { _orderService = orderService; }

        // FIX #3: Safe UserId extraction
        private bool TryGetUserId(out Guid userId)
        {
            var idClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.Identity?.Name;
            return Guid.TryParse(idClaim, out userId);
        }
        private bool IsAdmin => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _orderService.GetUserOrdersAsync(userId, page, pageSize));
        }

        [HttpGet("all"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] OrderStatus? status = null) =>
            Ok(await _orderService.GetAllAsync(page, pageSize, status));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _orderService.GetByIdAsync(id, IsAdmin ? null : userId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _orderService.CreateFromCartAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        //[HttpPut("{id:guid}/status"), Authorize(Roles = "Admin")]
        //public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        //{
        //    if (!TryGetUserId(out var userId)) return Unauthorized();
        //    var result = await _orderService.UpdateStatusAsync(id, dto, userId);
        //    return result.Success ? Ok(result) : BadRequest(result);
        //}
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]   // ??? ????
        public async Task<ApiResponse<OrderDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null)
                return ApiResponse<OrderDto>.Fail("?????? ??? ?????");

            // ??? ???? ?????? ?? ??????
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(adminIdClaim, out Guid adminId))
                return ApiResponse<OrderDto>.Fail("??? ???? ??");

            var result = await _orderService.UpdateStatusAsync(id, dto, adminId);

            return result;
        }
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _orderService.CancelOrderAsync(id, userId));
        }
    }
}
