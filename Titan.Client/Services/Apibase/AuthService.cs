using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Titan.Client.Services.Models;
using System.Text.Json;
using Titan.Client.Services.AppServices;

namespace Titan.Client.Services.Apibase
{
    public class AuthService : ApiBase
    {
        private readonly AuthenticationStateProvider _asp;
        public event Action? StateChanged;

        public UserDto? User { get; private set; }
        public bool IsAuthenticated => User is not null;
        public bool IsAdmin => User?.Role == "Admin";

        public AuthService(HttpClient h, ILocalStorageService s, NavigationManager n, AuthenticationStateProvider asp) : base(h, s, n) 
        { 
            _asp = asp;
        }

        public async Task InitAsync()
        { 
            User = await LoadStoredUserAsync(); 
            StateChanged?.Invoke(); 
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var token = await Storage.GetItemAsStringAsync(TK);
            return token;
        }

        public async Task<ApiResult<AuthResponse>?> LoginAsync(string email, string password)
        {
            var r = await PostAsync<AuthResponse>("api/auth/login", new { email, password });
            if (r?.Success == true && r.Data is not null) 
            { 
                await SaveAuthAsync(r.Data); 
                User = r.Data.User; 
                ((CustomAuthenticationStateProvider)_asp).NotifyUserAuthentication(JsonSerializer.Serialize(User, J.Opts));
                StateChanged?.Invoke(); 
            }
            return r;
        }

        public async Task<ApiResult<AuthResponse>?> RegisterAsync(string firstName, string lastName, string email, string phone, string password)
        {
            var r = await PostAsync<AuthResponse>("api/auth/register", new { firstName, lastName, email, phone, password });
            if (r?.Success == true && r.Data is not null) 
            { 
                await SaveAuthAsync(r.Data); 
                User = r.Data.User; 
                ((CustomAuthenticationStateProvider)_asp).NotifyUserAuthentication(JsonSerializer.Serialize(User, J.Opts));
                StateChanged?.Invoke(); 
            }
            return r;
        }

        public async Task LogoutAsync()
        {
            try {
                var rt = await Storage.GetItemAsStringAsync(RTK);
                if (!string.IsNullOrEmpty(rt)) await PostAsync<bool>("api/auth/logout", rt);
            } catch {}
            
            await ClearAuthAsync();
            User = null;
            ((CustomAuthenticationStateProvider)_asp).NotifyUserLogout();
            StateChanged?.Invoke();
            Nav.NavigateTo("/");
        }

        public async Task<bool> RefreshAsync()
        {
            var rt = await Storage.GetItemAsStringAsync(RTK);
            var at = await Storage.GetItemAsStringAsync(TK);
            if (string.IsNullOrEmpty(rt)) return false;
            
            var r = await PostAsync<AuthResponse>("api/auth/refresh-token", new { accessToken = at, refreshToken = rt });
            if (r?.Success == true && r.Data is not null) 
            { 
                await SaveAuthAsync(r.Data); 
                User = r.Data.User; 
                ((CustomAuthenticationStateProvider)_asp).NotifyUserAuthentication(JsonSerializer.Serialize(User, J.Opts));
                StateChanged?.Invoke(); 
                return true; 
            }
            return false;
        }

        public async Task<ApiResult<bool>?> ChangePasswordAsync(string current, string newPwd, string confirm)
            => await PostAsync<bool>("api/auth/change-password", new { currentPassword = current, newPassword = newPwd, confirmPassword = confirm });
    }
}
