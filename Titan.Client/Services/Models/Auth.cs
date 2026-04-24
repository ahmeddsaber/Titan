namespace Titan.Client.Services.Models
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime AccessTokenExpiry { get; set; }
        public UserDto User { get; set; } = new();
    }
}
