using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.DTOs.Product;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService) { _productService = productService; }

        private Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter) =>
            Ok(await _productService.GetAllAsync(filter, CurrentUserId));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id) =>
            Ok(await _productService.GetByIdAsync(id, CurrentUserId));

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug) =>
            Ok(await _productService.GetBySlugAsync(slug, CurrentUserId));

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeatured([FromQuery] int count = 8) =>
            Ok(await _productService.GetFeaturedAsync(count));

        [HttpGet("{id:guid}/related")]
        public async Task<IActionResult> GetRelated(Guid id, [FromQuery] int count = 4) =>
            Ok(await _productService.GetRelatedAsync(id, count));

        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var result = await _productService.CreateAsync(dto);
            return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result) : BadRequest(result);
        }

        [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/view"), Authorize]
        public async Task<IActionResult> RecordView(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            await _productService.RecordViewAsync(id, userId);
            return NoContent();
        }
    

}
}
