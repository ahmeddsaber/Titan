using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Titan.Infrastructure.Hubs
{
    /// <summary>
    /// Custom User ID Provider for SignalR to correctly extract the User ID from JWT Claims.
    /// Necessary when inbound claim mapping is cleared (DefaultInboundClaimTypeMap.Clear()) 
    /// as it prevents "sub" from mapping to standard NameIdentifier.
    /// </summary>
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Extract from standard 'sub' claim first, then fallback to NameIdentifier, Name, or Identity
            return connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.Identity?.Name;
        }
    }
}
