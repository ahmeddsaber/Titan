using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Titan.Client.Services.AppServices;
using Titan.Client.Services.Models;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Titan.Client.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register LocalStorage
builder.Services.AddBlazoredLocalStorage();

// API URL Configuration
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://titans.runasp.net/";

// Register JWT Handler
builder.Services.AddTransient<JwtHandler>();

// Configure HttpClient with JwtHandler
builder.Services.AddHttpClient("TitanAPI", client => 
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtHandler>();

// Standard HttpClient uses the factory
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("TitanAPI"));

// Authentication Services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Services
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

builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<SignalRService>();

await builder.Build().RunAsync();