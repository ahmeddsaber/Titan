using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin => User.IsInRole("Admin");
        public OrdersController(IOrderService orderService) { _orderService = orderService; }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            Ok(await _orderService.GetUserOrdersAsync(UserId, page, pageSize));

        [HttpGet("all"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] OrderStatus? status = null) =>
            Ok(await _orderService.GetAllAsync(page, pageSize, status));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _orderService.GetByIdAsync(id, IsAdmin ? null : UserId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderDto dto)
        {
            var result = await _orderService.CreateFromCartAsync(UserId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}/status"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var result = await _orderService.UpdateStatusAsync(id, dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id) =>
            Ok(await _orderService.CancelOrderAsync(id, UserId));
    }

  
}
