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
                Name = dto.ShippingInfo.FullName,
                Address = new AddressOptions
                {
                    Line1 = dto.ShippingInfo.Address,
                    City = dto.ShippingInfo.City,
                    State = dto.ShippingInfo.State,
                    PostalCode = dto.ShippingInfo.PostalCode,
                    Country = dto.ShippingInfo.Country
                }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        // Create pending order
        dto.PaymentIntentId = paymentIntent.Id;  // add payment intent to the dto
        var order = await _orderService.CreateOrderAsync(dto, user.Id); // create the order

        return Ok(new
        {
            clientSecret = paymentIntent.ClientSecret,
            paymentIntentId = paymentIntent.Id,
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
