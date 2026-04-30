using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Titan.Domain.Entities;
using Titan.Domain.Enum;

namespace Titan.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await SeedAdminUserAsync(context);
            await SeedCategoriesAsync(context);
            await SeedFullTestDataAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
        }
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context)
    {
        const string adminEmail = "admin@titan.com";
        
        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
            return;

        var adminUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FirstName = "TITAN",
            LastName = "Admin",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Secure runtime hashing
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new Category 
            { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), 
                Name = "Men", 
                NameAr = "رجال", 
                Slug = "men", 
                DisplayOrder = 1, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            },
            new Category 
            { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), 
                Name = "Women", 
                NameAr = "نساء", 
                Slug = "women", 
                DisplayOrder = 2, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            },
            new Category 
            { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), 
                Name = "Accessories", 
                NameAr = "إكسسوارات", 
                Slug = "accessories", 
                DisplayOrder = 3, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            }
        };


        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFullTestDataAsync(ApplicationDbContext context)
    {
        var random = new Random();

        // ================= USERS =================
        var users = new List<User>();

        for (int i = 1; i <= 20; i++)
        {
            var email = $"user{i}@test.com";

            if (await context.Users.AnyAsync(u => u.Email == email))
                continue;

            users.Add(new User
            {
                FirstName = "User",
                LastName = i.ToString(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Role = "Customer"
            });
        }

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var allUsers = await context.Users.ToListAsync();

        // ================= CATEGORY =================
        var category = await context.Categories.FirstAsync(c => c.Slug == "men");

        // ================= PRODUCTS =================
        var productNames = new List<(string en, string ar)>
        {
            ("Classic Cotton Shirt","قميص قطني كلاسيك"),
            ("Slim Fit Jeans","بنطلون جينز سليم فيت"),
            ("Casual Sneakers","كوتشي كاجوال"),
            ("Training Shoes","حذاء رياضي"),
            ("Basic T-Shirt","تيشيرت ساده"),
            ("Hoodie","هودي"),
            ("Leather Wallet","محفظة جلد"),
            ("Wrist Watch","ساعة يد"),
            ("Summer Dress","فستان صيفي"),
            ("Casual Blazer","بليزر"),
            ("Running Shorts","شورت رياضي"),
            ("Denim Jacket","جاكيت جينز"),
            ("Pajama Set","بيجامة"),
            ("Formal Shoes","حذاء رسمي"),
            ("Backpack","شنطة ظهر"),
            ("Cap","كاب"),
            ("Winter Coat","معطف"),
            ("Loose Pants","بنطلون واسع"),
            ("Tank Top","فانلة"),
            ("Slip-On Shoes","حذاء سهل")
        };

        var products = new List<Product>();

        foreach (var p in productNames)
        {
            var exists = await context.Products.AnyAsync(x => x.Name == p.en);
            if (exists) continue;

            var slug = p.en.ToLower().Replace(" ", "-").Replace("'", "");
            var baseSlug = slug;
            int counter = 1;
            while (await context.Products.AnyAsync(x => x.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var product = new Product
            {
                Name = p.en,
                NameAr = p.ar,
                Description = "منتج عالي الجودة مناسب للاستخدام اليومي",
                Slug = slug,
                Price = random.Next(200, 2000),
                DiscountPrice = random.Next(100, 150),
                StockQuantity = random.Next(5, 50),
                SKU = $"REAL-{Guid.NewGuid().ToString().Substring(0, 5)}",
                CategoryId = category.Id,
                MainImageUrl = $"https://picsum.photos/seed/{Guid.NewGuid()}/500/500"
            };

            products.Add(product);
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var allProducts = await context.Products.ToListAsync();

        // ================= IMAGES =================
        var images = new List<ProductImage>();

        foreach (var product in allProducts)
        {
            if (await context.ProductImages.AnyAsync(i => i.ProductId == product.Id))
                continue;

            for (int i = 1; i <= 3; i++)
            {
                images.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = $"https://picsum.photos/seed/{product.Id}-{i}/500/500",
                    IsPrimary = i == 1
                });
            }
        }

        context.ProductImages.AddRange(images);

        // ================= COUPONS =================
        if (!await context.Coupons.AnyAsync())
        {
            context.Coupons.AddRange(new List<Coupon>
            {
                new Coupon { Code="SALE10", DiscountType=DiscountType.Percentage, DiscountValue=10, IsActive=true },
                new Coupon { Code="SALE20", DiscountType=DiscountType.Percentage, DiscountValue=20, IsActive=true }
            });
        }

        // ================= ORDERS =================
        foreach (var user in allUsers.Take(10))
        {
            var product = allProducts[random.Next(allProducts.Count)];

            var order = new Order
            {
                OrderNumber = $"ORD-{Guid.NewGuid().ToString().Substring(0, 6)}",
                UserId = user.Id,
                SubTotal = product.Price,
                ShippingCost = 50,
                TotalAmount = product.Price + 50,
                ShippingFullName = user.FullName,
                ShippingAddress = "Sohag",
                ShippingCity = "Sohag",
                ShippingCountry = "Egypt",
                ShippingPhone = "01000000000"
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.Price,
                TotalPrice = product.Price
            });
        }

        // ================= REVIEWS =================
        foreach (var user in allUsers)
        {
            var product = allProducts[random.Next(allProducts.Count)];

            if (!await context.Reviews.AnyAsync(r => r.UserId == user.Id && r.ProductId == product.Id))
            {
                context.Reviews.Add(new Review
                {
                    UserId = user.Id,
                    ProductId = product.Id,
                    Rating = random.Next(3, 5),
                    Comment = "منتج ممتاز وأنصح به"
                });
            }
        }

        // ================= WISHLIST =================
        foreach (var user in allUsers)
        {
            var product = allProducts[random.Next(allProducts.Count)];

            if (!await context.WishlistItems.AnyAsync(w => w.UserId == user.Id && w.ProductId == product.Id))
            {
                context.WishlistItems.Add(new WishlistItem
                {
                    UserId = user.Id,
                    ProductId = product.Id
                });
            }
        }

        // ================= NOTIFICATIONS =================
        context.Notifications.AddRange(allUsers.Select(u => new Notification
        {
            UserId = u.Id,
            Title = "Welcome",
            Message = "أهلاً بيك في Titan 👋",
            Type = NotificationType.Welcome
        }));

        // ================= PRODUCT VIEWS =================
        context.ProductViews.AddRange(allUsers.Select(u => new ProductView
        {
            UserId = u.Id,
            ProductId = allProducts[random.Next(allProducts.Count)].Id,
            ViewCount = random.Next(1, 20)
        }));

        await context.SaveChangesAsync();
    }
}
