using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using CardShop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Controllers
{
    [ApiController]
    [Route("api/product")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductQueryObject query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } // validate model state

            var products = await _productService.GetAllAsync(query);
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } // validate model state

            var product = await _productService.GetByIdAsync(id);

            if (product == null) {
                return NotFound();
            }

            return Ok(product);
        } // end get product by id

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } // validate model state


            var productModel = productDto.ToProduct();

            // add logic for cloudingary later

            await _productService.CreateAsync(productModel);

            return CreatedAtAction(nameof(GetProductById), new { id = productModel.Id }, productModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromForm] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } // validate model state 

            var productModel = await _productService.UpdateAsync(id, updateDto);

            if (productModel == null) 
            {
                return NotFound();
            }

            return Ok(productModel);
        } // end update product

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _productService.DeleteAsync(id);

            if (product == null)
            {
                return NotFound(); 
            }

            return NoContent();
        } // end delete product


    } // end controller
} // end namespace
