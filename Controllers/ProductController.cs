using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    [ApiController]
    [Route("api/product")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // The logged-in admin's id, for the audit log lines below. Who is making the
        // request is an HTTP/auth concern, so it is read here and not in the service.
        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductQueryObject queryObject)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.GetAllAsync(queryObject);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateProductDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.CreateAsync(productDto);

            _logger.LogInformation("Admin {AdminId} created product {ProductId} ('{ProductName}').",
                GetUserId(), product.Id, product.Name);

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromForm] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.UpdateAsync(id, updateDto);
            if (product == null)
                return NotFound();

            _logger.LogInformation("Admin {AdminId} updated product {ProductId}.", GetUserId(), id);

            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // A product with order history makes the service throw ConflictException,
            // which the global exception middleware turns into a 409 response.
            var product = await _productService.DeleteAsync(id);
            if (product == null)
                return NotFound();

            _logger.LogInformation("Admin {AdminId} deleted product {ProductId}.", GetUserId(), id);

            return NoContent();
        }
    }
}