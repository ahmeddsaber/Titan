using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Titan.Client.Services.Models;

using Titan.Client.Services.Apibase;

namespace Titan.Client.Services.AppServices
{
 

// ════════════ PRODUCT ════════════════════════════════════════
public class ProductService : ApiBase
{
    public ProductService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<PagedList<ProductDto>?> GetAllAsync(ProductFilter f)
        => (await GetAsync<PagedList<ProductDto>>($"api/products{Q(f)}"))?.Data;

    public async Task<ProductDto?> GetBySlugAsync(string slug)
        => (await GetAsync<ProductDto>($"api/products/slug/{Uri.EscapeDataString(slug)}"))?.Data;

    public async Task<ProductDto?> GetByIdAsync(Guid id)
        => (await GetAsync<ProductDto>($"api/products/{id}"))?.Data;

    public async Task<List<ProductDto>?> GetFeaturedAsync(int count = 8)
        => (await GetAsync<List<ProductDto>>($"api/products/featured?count={count}"))?.Data;

    public async Task<List<ProductDto>?> GetRelatedAsync(Guid id, int count = 4)
        => (await GetAsync<List<ProductDto>>($"api/products/{id}/related?count={count}"))?.Data;

    public async Task<ApiResult<ProductDto>?> CreateAsync(object dto)
        => await PostAsync<ProductDto>("api/products", dto);

    public async Task<ApiResult<ProductDto>?> UpdateAsync(Guid id, object dto)
        => await PutAsync<ProductDto>($"api/products/{id}", dto);

    public async Task<bool> DeleteAsync(Guid id)
        => (await DelAsync<bool>($"api/products/{id}"))?.Success ?? false;

    public async Task RecordViewAsync(Guid id)
        => await PostAsync<bool>($"api/products/{id}/view");
}

// ════════════ CATEGORY ═══════════════════════════════════════
public class CategoryService : ApiBase
{
    public List<CategoryDto> All { get; private set; } = new();

    public CategoryService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task LoadAsync()
    { All = (await GetAsync<List<CategoryDto>>("api/categories"))?.Data ?? new(); }

    public async Task<ApiResult<CategoryDto>?> CreateAsync(object dto)
        => await PostAsync<CategoryDto>("api/categories", dto);

    public async Task<ApiResult<CategoryDto>?> UpdateAsync(Guid id, object dto)
        => await PutAsync<CategoryDto>($"api/categories/{id}", dto);

    public async Task<bool> DeleteAsync(Guid id)
        => (await DelAsync<bool>($"api/categories/{id}"))?.Success ?? false;
}

// ════════════ CART ═══════════════════════════════════════════
public class CartService : ApiBase
{
    public event Action? Changed;
    public CartSummaryDto? Current   { get; private set; }
    public int             Count     => Current?.TotalItems ?? 0;

    public CartService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<CartSummaryDto?> LoadAsync(string? coupon = null)
    {
        var url = coupon is null ? "api/cart" : $"api/cart?coupon={Uri.EscapeDataString(coupon)}";
        Current = (await GetAsync<CartSummaryDto>(url))?.Data;
        Changed?.Invoke();
        return Current;
    }

    public async Task<ApiResult<CartItemDto>?> AddAsync(Guid productId, int qty = 1, Guid? variantId = null)
    {
        var r = await PostAsync<CartItemDto>("api/cart/add", new { productId, quantity = qty, variantId });
        if (r?.Success == true) await LoadAsync(Current?.AppliedCouponCode);
        return r;
    }

    public async Task<bool> UpdateQtyAsync(Guid itemId, int qty)
    {
        var r = await PutAsync<CartItemDto>("api/cart/update", new { cartItemId = itemId, quantity = qty });
        if (r?.Success == true) await LoadAsync(Current?.AppliedCouponCode);
        return r?.Success ?? false;
    }

    public async Task<bool> RemoveAsync(Guid itemId)
    {
        var r = await DelAsync<bool>($"api/cart/{itemId}");
        if (r?.Success == true) await LoadAsync(Current?.AppliedCouponCode);
        return r?.Success ?? false;
    }

    public async Task<bool> ClearAsync()
    {
        var r = await DelAsync<bool>("api/cart/clear");
        if (r?.Success == true) { Current = null; Changed?.Invoke(); }
        return r?.Success ?? false;
    }

    public async Task<ApiResult<CouponDto>?> ValidateCouponAsync(string code)
        => await PostAsync<CouponDto>("api/cart/validate-coupon", new { couponCode = code });

    public async Task<bool> ApplyCouponAsync(string code)
    {
        var r = await ValidateCouponAsync(code);
        if (r?.Success == true) { await LoadAsync(code); return true; }
        return false;
    }
}

// ════════════ ORDER ══════════════════════════════════════════
public class OrderService : ApiBase
{
    public OrderService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<PagedList<OrderDto>?> GetMyOrdersAsync(int page = 1, int size = 10)
        => (await GetAsync<PagedList<OrderDto>>($"api/orders?page={page}&pageSize={size}"))?.Data;

    public async Task<OrderDto?> GetByIdAsync(Guid id)
        => (await GetAsync<OrderDto>($"api/orders/{id}"))?.Data;

    public async Task<ApiResult<OrderDto>?> CheckoutAsync(object req)
        => await PostAsync<OrderDto>("api/orders/checkout", req);

    public async Task<bool> CancelAsync(Guid id)
        => (await PostAsync<bool>($"api/orders/{id}/cancel"))?.Success ?? false;

    // admin
    public async Task<PagedList<OrderDto>?> GetAllAsync(int page = 1, int size = 20, OrderStatus? status = null)
    {
        var url = $"api/orders/all?page={page}&pageSize={size}";
        if (status.HasValue) url += $"&status={(int)status.Value}";
        return (await GetAsync<PagedList<OrderDto>>(url))?.Data;
    }

    public async Task<ApiResult<OrderDto>?> UpdateStatusAsync(Guid id, OrderStatus status, string? note = null)
        => await PutAsync<OrderDto>($"api/orders/{id}/status", new { status, note });
}

// ════════════ WISHLIST ════════════════════════════════════════
public class WishlistService : ApiBase
{
    public event Action?        Changed;
    public List<ProductDto>     Items { get; private set; } = new();
    public int                  Count => Items.Count;

    public WishlistService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task LoadAsync()
    { Items = (await GetAsync<List<ProductDto>>("api/wishlist"))?.Data ?? new(); Changed?.Invoke(); }

    public bool Has(Guid id) => Items.Any(p => p.Id == id);

    public async Task<bool> ToggleAsync(Guid productId)
    {
        var wasIn = Has(productId);
        var r = wasIn
            ? await DelAsync<bool>($"api/wishlist/{productId}")
            : await PostAsync<bool>($"api/wishlist/{productId}");
        if (r?.Success == true) await LoadAsync();
        return !wasIn;
    }
}

// ════════════ REVIEW ══════════════════════════════════════════
public class ReviewService : ApiBase
{
    public ReviewService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<PagedList<ReviewDto>?> GetProductReviewsAsync(Guid productId, int page = 1)
        => (await GetAsync<PagedList<ReviewDto>>($"api/reviews/product/{productId}?page={page}&pageSize=10"))?.Data;

    public async Task<ApiResult<ReviewDto>?> CreateAsync(Guid productId, int rating, string comment)
        => await PostAsync<ReviewDto>("api/reviews", new { productId, rating, comment });

    public async Task<bool> DeleteAsync(Guid id)
        => (await DelAsync<bool>($"api/reviews/{id}"))?.Success ?? false;

    public async Task<bool> HelpfulAsync(Guid id)
        => (await PostAsync<bool>($"api/reviews/{id}/helpful"))?.Success ?? false;
}

// ════════════ NOTIFICATION ════════════════════════════════════
public class NotificationService : ApiBase
{
    public event Action?           Changed;
    public List<NotificationDto>   Items       { get; private set; } = new();
    public int                     UnreadCount => Items.Count(n => !n.IsRead);

    public NotificationService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task LoadAsync(int count = 30)
    { Items = (await GetAsync<List<NotificationDto>>($"api/notifications?count={count}"))?.Data ?? new(); Changed?.Invoke(); }

    public async Task MarkReadAsync(Guid id)
    {
        await PutAsync<bool>($"api/notifications/{id}/read");
        var n = Items.FirstOrDefault(x => x.Id == id);
        if (n is not null) n.IsRead = true;
        Changed?.Invoke();
    }

    public async Task MarkAllReadAsync()
    {
        await PutAsync<bool>("api/notifications/mark-all-read");
        foreach (var n in Items) n.IsRead = true;
        Changed?.Invoke();
    }

    public void PushRealTime(NotificationDto dto) { Items.Insert(0, dto); Changed?.Invoke(); }
}

// ════════════ USER ════════════════════════════════════════════
public class UserService : ApiBase
{
    public UserService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<UserDto?> GetMeAsync()
        => (await GetAsync<UserDto>("api/users/me"))?.Data;

    public async Task<ApiResult<UserDto>?> UpdateProfileAsync(string firstName, string lastName, string phone, string lang)
        => await PutAsync<UserDto>("api/users/profile", new { firstName, lastName, phone, preferredLanguage = lang });

    public async Task<PagedList<UserDto>?> GetAllAsync(int page = 1, int size = 20, string? search = null)
    {
        var url = $"api/users?page={page}&pageSize={size}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return (await GetAsync<PagedList<UserDto>>(url))?.Data;
    }

    public async Task<bool> BanAsync(Guid id, string reason)
        => (await PostAsync<bool>($"api/users/{id}/ban", new { reason }))?.Success ?? false;

    public async Task<bool> UnbanAsync(Guid id)
        => (await PostAsync<bool>($"api/users/{id}/unban"))?.Success ?? false;

    public async Task<bool> DeleteAsync(Guid id)
        => (await DelAsync<bool>($"api/users/{id}"))?.Success ?? false;

    public async Task<DashboardStats?> GetStatsAsync()
        => (await GetAsync<DashboardStats>("api/users/dashboard-stats"))?.Data;
}

// ════════════ COUPON ══════════════════════════════════════════
public class CouponService : ApiBase
{
    public CouponService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<PagedList<CouponDto>?> GetAllAsync(int page = 1)
        => (await GetAsync<PagedList<CouponDto>>($"api/coupons?page={page}&pageSize=30"))?.Data;

    public async Task<ApiResult<CouponDto>?> CreateAsync(object dto)
        => await PostAsync<CouponDto>("api/coupons", dto);

    public async Task<bool> DeleteAsync(Guid id)
        => (await DelAsync<bool>($"api/coupons/{id}"))?.Success ?? false;

    public async Task<bool> ToggleAsync(Guid id)
        => (await PostAsync<bool>($"api/coupons/{id}/toggle"))?.Data ?? false;
}

// ════════════ RECOMMENDATION ══════════════════════════════════
public class RecommendationService : ApiBase
{
    public RecommendationService(HttpClient h, IJSRuntime j, NavigationManager n) : base(h, j, n) { }

    public async Task<RecommendationResult?> GetAsync(int count = 8)
        => (await GetAsync<RecommendationResult>($"api/recommendations?count={count}"))?.Data;
}

// ════════════ TOAST ═══════════════════════════════════════════
public class ToastService
{
    public record Toast(Guid Id, string Message, string Type);
    public event Action? Changed;
    public List<Toast>   Items { get; } = new();

    public void Show(string msg, string type = "info")
    {
        var t = new Toast(Guid.NewGuid(), msg, type);
        Items.Add(t); Changed?.Invoke();
        _ = Task.Delay(4000).ContinueWith(_ => { Items.RemoveAll(x => x.Id == t.Id); Changed?.Invoke(); });
    }
    public void Success(string m) => Show(m, "success");
    public void Error(string m)   => Show(m, "error");
    public void Info(string m)    => Show(m, "info");
    public void Warn(string m)    => Show(m, "warning");
}

// ════════════ SIGNALR ═════════════════════════════════════════
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hub;
    private bool           _connected;

    public event Action<NotificationDto>? OnNotification;
    public event Action<OrderDto>?        OnOrderUpdate;
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string baseUrl, string token)
    {
        if (_connected) return;
        _hub = new HubConnectionBuilder()
            .WithUrl(baseUrl.TrimEnd('/') + "/hubs/titan", o => o.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<NotificationDto>("ReceiveNotification", dto => OnNotification?.Invoke(dto));
        _hub.On<OrderDto>("OrderStatusUpdated",         dto => OnOrderUpdate?.Invoke(dto));

        try { await _hub.StartAsync(); _connected = true; }
        catch (Exception ex) { Console.WriteLine($"[SignalR] {ex.Message}"); }
    }

    public async Task DisconnectAsync()
    { if (_hub is not null) await _hub.StopAsync(); _connected = false; }

    public async ValueTask DisposeAsync()
    { if (_hub is not null) await _hub.DisposeAsync(); }
}
}
