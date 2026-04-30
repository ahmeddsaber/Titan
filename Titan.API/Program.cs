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
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Support JWT for SignalR Hubs
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
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

    // 6. Controllers, SignalR, and CORS
    builder.Services.AddControllers()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddSignalR();
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

    builder.Services.AddCors(o => o.AddPolicy("TitanPolicy", p =>
        p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

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

    // 8. Middleware Pipeline (Order is Important)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TITAN API v1");
        c.RoutePrefix = "";
        //c.RoutePrefix = "swagger"; // Opens at /swagger
    });

    // app.UseHttpsRedirection(); // Causes 405 on SignalR negotiation redirects
    app.UseCors("TitanPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<TitanHub>("/hubs/titan");
    app.MapHealthChecks("/health");
    app.MapGet("/", () => "TITAN API is running ??");
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