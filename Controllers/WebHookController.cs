using api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _config;

    public StripeWebhookController(IOrderService orderService, IConfiguration config)
    {
        _orderService = orderService;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _config["Stripe:WebhookSecret"]
            );

            if (stripeEvent.Type == "payment_intent.failed") 
            {
                var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                if (failedIntent != null)
                {
                    await _orderService.MarkOrderFailedAsync(failedIntent.Id);
                }
            }


            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await _orderService.MarkOrderPaidAsync(paymentIntent.Id);
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
