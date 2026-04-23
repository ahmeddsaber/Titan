using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Auth;
using Titan.Application.DTOs.User;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Domain.Enum;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services;

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly INotificationService _notificationService;

        public AuthService(ApplicationDbContext db, ITokenService tokenService, INotificationService notificationService)
        {
            _db = db;
            _tokenService = tokenService;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, string ipAddress)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
                return ApiResponse<AuthResponseDto>.Fail("Email already in use.");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLower().Trim(),
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            _db.Users.Add(user);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = _tokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedByIp = ipAddress
            };
            _db.RefreshTokens.Add(refreshToken);

            await _db.SaveChangesAsync();

            // Welcome notification
            await _notificationService.SendNotificationAsync(
                user.Id, "Welcome to TITAN! 🔱",
                $"Welcome {user.FirstName}! Your journey with TITAN begins now. Unleash the TITAN within.",
                NotificationType.Welcome);

            return ApiResponse<AuthResponseDto>.Ok(BuildAuthResponse(user, refreshToken.Token), "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

            if (user.IsBanned)
                return ApiResponse<AuthResponseDto>.Fail($"Account suspended. Reason: {user.BanReason}");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Fail("Account is inactive.");

            // Revoke old tokens
            var oldTokens = await _db.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow).ToListAsync();
            foreach (var t in oldTokens) { t.IsRevoked = true; t.RevokedReason = "Replaced on login"; }

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = _tokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedByIp = ipAddress
            };
            _db.RefreshTokens.Add(refreshToken);

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResponse<AuthResponseDto>.Ok(BuildAuthResponse(user, refreshToken.Token), "Login successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            var token = await _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token == null || !token.IsActive)
                return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

            token.IsRevoked = true;
            token.RevokedReason = "Replaced by refresh";

            var newRefresh = new RefreshToken
            {
                UserId = token.UserId,
                Token = _tokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedByIp = ipAddress
            };
            token.ReplacedByToken = newRefresh.Token;
            _db.RefreshTokens.Add(newRefresh);

            await _db.SaveChangesAsync();
            return ApiResponse<AuthResponseDto>.Ok(BuildAuthResponse(token.User, newRefresh.Token));
        }

        public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedReason = "Logout";
                await _db.SaveChangesAsync();
            }
            return ApiResponse<bool>.Ok(true, "Logged out successfully.");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return ApiResponse<bool>.Fail("Passwords do not match.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return ApiResponse<bool>.Fail("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Password changed successfully.");
        }

        private AuthResponseDto BuildAuthResponse(User user, string refreshToken)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
                User = MapUser(user)
            };
        }

        private static UserDto MapUser(User u) => new()
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            Role = u.Role,
            ProfileImageUrl = u.ProfileImageUrl,
            IsActive = u.IsActive,
            IsBanned = u.IsBanned,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            PreferredLanguage = u.PreferredLanguage
        };
    }
    
 