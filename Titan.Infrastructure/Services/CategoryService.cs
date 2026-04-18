using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Application.DTOs.API_Response;
using Titan.Application.DTOs.Catogery;
using Titan.Application.Interfaces;
using Titan.Domain.Entities;
using Titan.Infrastructure.Data;

namespace Titan.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        public CategoryService(ApplicationDbContext db) { _db = db; }

        public async Task<ApiResponse<List<CategoryDto>>> GetAllAsync()
        {
            var categories = await _db.Categories
                .Include(c => c.Children).Include(c => c.Products)
                .Where(c => c.ParentId == null).AsNoTracking().ToListAsync();
            return ApiResponse<List<CategoryDto>>.Ok(categories.Select(MapCategory).ToList());
        }

        public async Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id)
        {
            var c = await _db.Categories.Include(c => c.Children).Include(c => c.Products)
                .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return c == null ? ApiResponse<CategoryDto>.Fail("Not found.") : ApiResponse<CategoryDto>.Ok(MapCategory(c));
        }

        public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto)
        {
            if (await _db.Categories.AnyAsync(c => c.Slug == dto.Slug))
                return ApiResponse<CategoryDto>.Fail("Slug already exists.");
            var category = new Category
            {
                Name = dto.Name,
                NameAr = dto.NameAr,
                Slug = dto.Slug,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                ParentId = dto.ParentId,
                DisplayOrder = dto.DisplayOrder
            };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return ApiResponse<CategoryDto>.Ok(MapCategory(category));
        }

        public async Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return ApiResponse<CategoryDto>.Fail("Not found.");
            category.Name = dto.Name; category.NameAr = dto.NameAr; category.Description = dto.Description;
            category.ImageUrl = dto.ImageUrl; category.IsActive = dto.IsActive; category.DisplayOrder = dto.DisplayOrder;
            await _db.SaveChangesAsync();
            return ApiResponse<CategoryDto>.Ok(MapCategory(category));
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return ApiResponse<bool>.Fail("Not found.");
            if (category.Products.Any()) return ApiResponse<bool>.Fail("Cannot delete category with products.");
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        private static CategoryDto MapCategory(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            NameAr = c.NameAr,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ParentId = c.ParentId,
            DisplayOrder = c.DisplayOrder,
            ProductCount = c.Products?.Count ?? 0,
            Children = c.Children?.Select(MapCategory).ToList() ?? new()
        };
    }
}
