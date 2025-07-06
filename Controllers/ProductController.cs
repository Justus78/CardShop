using api.DTOs.Product;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Services;
using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
        private readonly IPhotoService _photoService;

        public ProductController(IProductService productService, IPhotoService photoService)
        {
            _productService = productService;
            _photoService = photoService;
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } // validate model state


            var productModel = productDto.ToProduct();

            if (productDto.ProductImage != null) // if profilePic isnt null
            {
                var result = await _photoService.AddPhotoAsync(productDto.ProductImage); // add the photo to cloudinary
                productModel.ImageUrl = result.Url.ToString(); // add the url from cloudinary to the product model
                productModel.CloudinaryId = result.PublicId.ToString();
            }

            // add logic for cloudingary later

            await _productService.CreateAsync(productModel);

            return CreatedAtAction(nameof(GetProductById), new { id = productModel.Id }, productModel);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
