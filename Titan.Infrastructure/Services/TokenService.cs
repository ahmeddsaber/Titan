using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;

namespace Titan.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenMinutes;

        public TokenService(IConfiguration config)
        {
            _config = config;
            _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            _issuer = config["Jwt:Issuer"] ?? "titan-api";
            _audience = config["Jwt:Audience"] ?? "titan-client";
            _accessTokenMinutes = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        }

        public string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("profileImage", user.ProfileImageUrl ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public Guid? GetUserIdFromToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.InboundClaimTypeMap.Clear(); // Ensure consistent claim mapping

                var key = Encoding.UTF8.GetBytes(_secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false // Allow extraction from expired tokens
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                
                // Try to find the user ID claim in various possible formats
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
                    c.Type == JwtRegisteredClaimNames.Sub || 
                    c.Type == "sub" || 
                    c.Type == ClaimTypes.NameIdentifier);

                return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : null;
            }
            catch
            {
                return null;
            }
        }
    }
    }
