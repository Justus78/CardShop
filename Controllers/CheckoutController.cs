using api.DTOs.Order;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(ICheckoutService checkoutService,
        IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _checkoutService = checkoutService;
        _orderService = orderService;
        _userManager = userManager;
    }

    [HttpPost("create-payment-intent")]
    public async Task<ActionResult> CreatePaymentIntent(CreateOrderDto orderDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _checkoutService.CreatePaymentIntentAsync(orderDto, userId);

        return Ok(result); // returns { clientSecret, paymentIntentId }
    }

    [HttpPost("confirm-order")]
    public async Task<ActionResult> ConfirmOrder(CreateOrderDto orderDto) // removed param for paymentIntendId, moved to DTO
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var order = await _orderService.CreateOrderAsync(orderDto, userId);

        return Ok(order);
    }
}
