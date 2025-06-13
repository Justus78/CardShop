using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using api.DTOs.Cart;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(ICartService cartService, UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddCartItemDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var added = await _cartService.AddAsync(userId, dto);
        return added is null ? BadRequest("Unable to add item.") : Ok(added);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCartItemDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var updated = await _cartService.UpdateAsync(userId, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var ok = await _cartService.RemoveAsync(userId, id);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _cartService.ClearAsync(userId);
        return NoContent();
    }
}
