using Blazored.LocalStorage;
using global::Titan.Client.Services.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titan.Client.Services.Apibase
{
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

    public abstract class ApiBase
    {
        protected readonly HttpClient Http;
        protected readonly ILocalStorageService Storage;
        protected readonly NavigationManager Nav;

        protected const string TK = "titan_token";
        protected const string RTK = "titan_refresh";
        protected const string UK = "titan_user";

        protected ApiBase(HttpClient http, ILocalStorageService storage, NavigationManager nav)
        { 
            Http = http; 
            Storage = storage; 
            Nav = nav; 
        }

        protected async Task SaveAuthAsync(AuthResponse a)
        {
            await Storage.SetItemAsStringAsync(TK, a.AccessToken);
            await Storage.SetItemAsStringAsync(RTK, a.RefreshToken);
            await Storage.SetItemAsync(UK, a.User);
        }

        protected async Task ClearAuthAsync()
        {
            await Storage.RemoveItemAsync(TK);
            await Storage.RemoveItemAsync(RTK);
            await Storage.RemoveItemAsync(UK);
        }

        protected async Task<UserDto?> LoadStoredUserAsync()
        {
            return await Storage.GetItemAsync<UserDto>(UK);
        }

        protected async Task<ApiResult<T>?> GetAsync<T>(string url)
        {
            try { return await Read<T>(await Http.GetAsync(url)); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> PostAsync<T>(string url, object? body = null)
        {
            try { return await Read<T>(await Http.PostAsync(url, Ser(body))); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> PutAsync<T>(string url, object? body = null)
        {
            try { return await Read<T>(await Http.PutAsync(url, Ser(body))); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

        protected async Task<ApiResult<T>?> DelAsync<T>(string url)
        {
            try { return await Read<T>(await Http.DeleteAsync(url)); }
            catch (Exception e) { return Err<T>(e.Message); }
        }

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
}
