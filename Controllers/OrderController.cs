using api.DTOs.Order;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public OrdersController(IOrderService orderService, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _orderService = orderService;
        _userManager = userManager;
        _config = config;
    }

    [HttpPost("create-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateOrderDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(dto.Items.Sum(i => i.Quantity * i.UnitPrice) * 100),
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Shipping = new ChargeShippingOptions
            {
                Name = dto.RecipientName,
                Address = new AddressOptions
                {
                    Line1 = dto.Street,
                    City = dto.City,
                    State = dto.State,
                    PostalCode = dto.PostalCode,
                    Country = dto.Country
                }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        // Create pending order
        dto.PaymentIntentId = paymentIntent.Id;
        var order = await _orderService.CreateOrderAsync(dto, user.Id);

        return Ok(new
        {
            clientSecret = paymentIntent.ClientSecret,
            order
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var orders = await _orderService.GetOrdersForUserAsync(user.Id);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var order = await _orderService.GetOrderByIdAsync(id, user.Id);
        if (order == null) return NotFound();
        return Ok(order);
    }
}
