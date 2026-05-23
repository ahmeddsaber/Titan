using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.DTOs;
using Titan.Application.DTOs.User;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) { _userService = userService; }

    // FIX #3: Safe UserId extraction
    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.Identity?.Name, out userId);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _userService.GetByIdAsync(userId));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _userService.UpdateProfileAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await _userService.GetAllAsync(page, pageSize, search));

    [HttpGet("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _userService.GetByIdAsync(id));

    [HttpPost("{id:guid}/ban"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserRequest req)
    {
        var result = await _userService.BanUserAsync(id, req.Reason);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/unban"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Unban(Guid id) => Ok(await _userService.UnbanUserAsync(id));

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await _userService.DeleteUserAsync(id));

    [HttpGet("dashboard-stats"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DashboardStats() => Ok(await _userService.GetDashboardStatsAsync());
}

public record BanUserRequest(string Reason);