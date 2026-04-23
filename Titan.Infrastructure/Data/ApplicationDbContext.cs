using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Titan.Domain.Entities;

namespace Titan.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ProductView> ProductViews => Set<ProductView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 5. Fix EF Core relationship warnings caused by global query filters
        // Apply global soft delete filter to ALL entities that inherit from BaseEntity
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProductImage>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProductVariant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CartItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderStatusHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Coupon>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WishlistItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Review>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ActivityLog>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProductView>().HasQueryFilter(e => !e.IsDeleted);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired(false).HasMaxLength(50).HasDefaultValue("Customer");
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.SKU).IsUnique();
            // 4. Fix decimal warnings by properly setting precision
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.Category).WithMany(c => c.Products).HasForeignKey(e => e.CategoryId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        // ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.Property(e => e.PriceAdjustment).HasPrecision(18, 2);
        });

        // Category self-reference
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.Parent).WithMany(c => c.Children).HasForeignKey(e => e.ParentId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.VariantId }).IsUnique();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.User).WithMany(u => u.CartItems).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(p => p.CartItems).HasForeignKey(e => e.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.User).WithMany(u => u.Orders).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Coupon).WithMany(c => c.Orders).HasForeignKey(e => e.CouponId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(p => p.OrderItems).HasForeignKey(e => e.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        // Coupon
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.MinimumOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaximumDiscountAmount).HasPrecision(18, 2);
        });

        // WishlistItem
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.WishlistItems).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(p => p.WishlistItems).HasForeignKey(e => e.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasOne(e => e.User).WithMany(u => u.Reviews).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Product).WithMany(p => p.Reviews).HasForeignKey(e => e.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(e => e.User).WithMany(u => u.Notifications).HasForeignKey(e => e.UserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // ProductView
        modelBuilder.Entity<ProductView>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.ProductViews).HasForeignKey(e => e.UserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(p => p.ProductViews).HasForeignKey(e => e.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

    }


    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
