using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Titan.Client.Services;
using Titan.Client.Services.Apibase;
using Titan.Client.Services.AppServices;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Titan.Client.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ?? HTTP Client ???????????????????????????????????????????????
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBase),
    Timeout = TimeSpan.FromSeconds(30)
});

// ?? Scoped Services ???????????????????????????????????????????
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<WishlistService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CouponService>();
builder.Services.AddScoped<RecommendationService>();

// ?? Singleton Services (shared across components) ?????????????
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<SignalRService>();

await builder.Build().RunAsync();