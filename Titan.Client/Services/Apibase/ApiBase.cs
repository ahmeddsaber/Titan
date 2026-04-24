using global::Titan.Client.Services.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;




namespace Titan.Client.Services.Apibase
{
  

    // ════════════════════════════════════════════════════════════════
    //  JSON OPTIONS
    // ════════════════════════════════════════════════════════════════
    public static class J
    {
        public static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  API BASE
    // ════════════════════════════════════════════════════════════════
    public abstract class ApiBase
    {
        protected readonly HttpClient Http;
        protected readonly IJSRuntime JS;
        protected readonly NavigationManager Nav;

        private const string TK = "titan_token";
        private const string RTK = "titan_refresh";
        private const string UK = "titan_user";

        protected ApiBase(HttpClient http, IJSRuntime js, NavigationManager nav)
        { Http = http; JS = js; Nav = nav; }

        // ── token helpers ─────────────────────────────────────────
        protected async Task<string?> GetTokenAsync()
        { try { return await JS.InvokeAsync<string?>("localStorage.getItem", TK); } catch { return null; } }

        protected async Task<string?> GetRefreshTokenAsync()
        { try { return await JS.InvokeAsync<string?>("localStorage.getItem", RTK); } catch { return null; } }

        protected async Task SaveAuthAsync(AuthResponse a)
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", TK, a.AccessToken);
                await JS.InvokeVoidAsync("localStorage.setItem", RTK, a.RefreshToken);
                await JS.InvokeVoidAsync("localStorage.setItem", UK, JsonSerializer.Serialize(a.User, J.Opts));
            }
            catch { }
        }

        protected async Task ClearAuthAsync()
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.removeItem", TK);
                await JS.InvokeVoidAsync("localStorage.removeItem", RTK);
                await JS.InvokeVoidAsync("localStorage.removeItem", UK);
            }
            catch { }
        }

        protected async Task<UserDto?> LoadStoredUserAsync()
        {
            try
            {
                var json = await JS.InvokeAsync<string?>("localStorage.getItem", UK);
                return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<UserDto>(json, J.Opts);
            }
            catch { return null; }
        }

        // ── attach JWT ────────────────────────────────────────────
        protected async Task AuthAsync()
        {
            var t = await GetTokenAsync();
            Http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(t)
                ? null : new AuthenticationHeaderValue("Bearer", t);
        }

        // ── HTTP methods ──────────────────────────────────────────
        protected async Task<ApiResult<T>?> GetAsync<T>(string url)
        {
            await AuthAsync();
            try { return await Read<T>(await Http.GetAsync(url)); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> PostAsync<T>(string url, object? body = null)
        {
            await AuthAsync();
            try { return await Read<T>(await Http.PostAsync(url, Ser(body))); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> PutAsync<T>(string url, object? body = null)
        {
            await AuthAsync();
            try { return await Read<T>(await Http.PutAsync(url, Ser(body))); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> DelAsync<T>(string url)
        {
            await AuthAsync();
            try { return await Read<T>(await Http.DeleteAsync(url)); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        // ── query builder ─────────────────────────────────────────
        protected static string Q(ProductFilter f)
        {
            var sb = new StringBuilder($"?page={f.Page}&pageSize={f.PageSize}&sortBy={f.SortBy}");
            if (!string.IsNullOrWhiteSpace(f.Search)) sb.Append($"&search={Uri.EscapeDataString(f.Search)}");
            if (f.CategoryId.HasValue) sb.Append($"&categoryId={f.CategoryId}");
            if (f.MinPrice.HasValue) sb.Append($"&minPrice={f.MinPrice}");
            if (f.MaxPrice.HasValue) sb.Append($"&maxPrice={f.MaxPrice}");
            if (f.IsFeatured.HasValue) sb.Append($"&isFeatured={f.IsFeatured}");
            if (f.HasDiscount.HasValue) sb.Append($"&hasDiscount={f.HasDiscount}");
            return sb.ToString();
        }

        // ── internals ─────────────────────────────────────────────
        private static StringContent? Ser(object? b) =>
            b is null ? null : new StringContent(JsonSerializer.Serialize(b, J.Opts), Encoding.UTF8, "application/json");

        private static async Task<ApiResult<T>?> Read<T>(HttpResponseMessage r)
        {
            var raw = await r.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(raw)) return new ApiResult<T> { Success = r.IsSuccessStatusCode };
            try { return JsonSerializer.Deserialize<ApiResult<T>>(raw, J.Opts); }
            catch { return new ApiResult<T> { Success = false, Message = raw }; }
        }

        private static ApiResult<T> Err<T>(string msg) =>
            new() { Success = false, Message = msg };
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTH SERVICE
    // ════════════════════════════════════════════════════════════════
    public class AuthService : ApiBase
    {
        public event Action? StateChanged;

        public UserDto? User { get; private set; }
        public bool IsAuthenticated => User is not null;
        public bool IsAdmin => User?.Role == "Admin";

        public AuthService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

        public async Task InitAsync()
        { User = await LoadStoredUserAsync(); StateChanged?.Invoke(); }

        public async Task<ApiResult<AuthResponse>?> LoginAsync(string email, string password)
        {
            var r = await PostAsync<AuthResponse>("api/auth/login", new { email, password });
            if (r?.Success == true && r.Data is not null) { await SaveAuthAsync(r.Data); User = r.Data.User; StateChanged?.Invoke(); }
            return r;
        }

        public async Task<ApiResult<AuthResponse>?> RegisterAsync(string firstName, string lastName, string email, string phone, string password)
        {
            var r = await PostAsync<AuthResponse>("api/auth/register", new { firstName, lastName, email, phone, password });
            if (r?.Success == true && r.Data is not null) { await SaveAuthAsync(r.Data); User = r.Data.User; StateChanged?.Invoke(); }
            return r;
        }

        public async Task LogoutAsync()
        {
            var rt = await GetRefreshTokenAsync();
            if (!string.IsNullOrEmpty(rt)) await PostAsync<bool>("api/auth/logout", rt);
            await ClearAuthAsync();
            User = null;
            StateChanged?.Invoke();
            Nav.NavigateTo("/");
        }

        public async Task<bool> RefreshAsync()
        {
            var rt = await GetRefreshTokenAsync();
            var at = await GetTokenAsync();
            if (string.IsNullOrEmpty(rt)) return false;
            var r = await PostAsync<AuthResponse>("api/auth/refresh-token", new { accessToken = at, refreshToken = rt });
            if (r?.Success == true && r.Data is not null) { await SaveAuthAsync(r.Data); User = r.Data.User; StateChanged?.Invoke(); return true; }
            return false;
        }

        public async Task<ApiResult<bool>?> ChangePasswordAsync(string current, string newPwd, string confirm)
            => await PostAsync<bool>("api/auth/change-password", new { currentPassword = current, newPassword = newPwd, confirmPassword = confirm });

        public async Task<string?> GetAccessTokenAsync() => await GetTokenAsync();
    }
}
