using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Titan.Domain.Entities;

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
}
