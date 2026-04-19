using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Titan.Application.DTOs.Catogery;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService) { _categoryService = categoryService; }

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _categoryService.GetAllAsync());
        [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) => Ok(await _categoryService.GetByIdAsync(id));

        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto) => Ok(await _categoryService.CreateAsync(dto));

        [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto) => Ok(await _categoryService.UpdateAsync(id, dto));

        [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id) => Ok(await _categoryService.DeleteAsync(id));
    }

}
