using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.User;

namespace Titan.Application.DTOs.Auth;
public record RegisterDto(string FirstName, string LastName, string Email, string Phone, string Password);
public record LoginDto(string Email, string Password);
public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
public record RefreshTokenDto(string AccessToken, string RefreshToken);

public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public UserDto User { get; init; } = null!;
}
