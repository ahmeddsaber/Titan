using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Titan.Application.Interfaces;
using Titan.Infrastructure.Data;
using Titan.Infrastructure.Services;
using Titan.Infrastructure.Hubs;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

// 1. Initial Logger Setup
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TITAN API...");
    var builder = WebApplication.CreateBuilder(args);

    // 2. Serilog Configuration
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/titan-.log", rollingInterval: RollingInterval.Day));

    // 3. Database Connection
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sql => {
            sql.EnableRetryOnFailure();
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));

    // 4. JWT Authentication (Simple & Safe Fallback)
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "TITAN_FALLBACK_SECRET_KEY_FOR_DEVELOPMENT_32_CHARS";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "titan-api",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "titan-client",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = ClaimTypes.Role
            };

            // Support JWT for SignalR Hubs
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && 
                        (path.StartsWithSegments("/hubs") || (path.Value != null && path.Value.Contains("/negotiate"))))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddMemoryCache();

    // 5. Dependency Injection (Register Services)
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ICouponService, CouponService>();
    builder.Services.AddScoped<IWishlistService, WishlistService>();
    builder.Services.AddScoped<IReviewService, ReviewService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IUserService, UserService>();

    // 6. Controllers, SignalR, and Swagger
    builder.Services.AddControllers()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Titan.Infrastructure.Hubs.CustomUserIdProvider>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TITAN API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token only (no 'Bearer' prefix needed in this UI)"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    // ╔═══════════════════════════════════════════════════════════════╗
    // ║  FIX #1: CORS — ONE policy, specific origins + credentials  ║
    // ║  CAUSE: Two AddCors() calls overwrote each other, and       ║
    // ║         SetIsOriginAllowed(_ => true) is insecure.           ║
    // ╚═══════════════════════════════════════════════════════════════╝
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("TitanPolicy", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5213",
                    "https://localhost:5213",
                    "https://titanstore.runasp.net", // Added correct frontend URL
                    "https://titans.runasp.net"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // 7. Migration Logic (Safe: won't crash the app if DB is not ready)
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            
            // 7.1 Runtime Seeding
            await DbSeeder.SeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration or seeding failed. The app will continue starting.");
        }
    }

    // ╔═══════════════════════════════════════════════════════════════╗
    // ║  8. Middleware Pipeline — ORDER MATTERS!                     ║
    // ║  FIX #2: CORS must run BEFORE Authentication & Routing      ║
    // ║  FIX #3: Global error handler catches unhandled 500s        ║
    // ╚═══════════════════════════════════════════════════════════════╝

    // 8.0 Global Exception Handler (catches unhandled exceptions → returns JSON instead of 500)
    app.UseExceptionHandler(error =>
    {
        error.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            Log.Error(ex?.Error, "Unhandled exception on {Path}", context.Request.Path);
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An internal server error occurred. Please try again later.",
                detail = app.Environment.IsDevelopment() ? ex?.Error?.Message : null
            });
        });
    });

    // 8.1 Swagger (available in all environments for your hosted API)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TITAN API v1");
        c.RoutePrefix = "";
    });
  

    // 8.2 CORS — must be BEFORE auth and routing
    app.UseCors("TitanPolicy");

    // 8.3 Auth
    app.UseAuthentication();
    app.UseAuthorization();

    // 8.4 Endpoints
    app.MapControllers();
    app.MapHub<TitanHub>("/hubs/titan");
    app.MapHealthChecks("/health");
    app.MapGet("/ping", () => "TITAN API is running 🚀");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application crashed unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}