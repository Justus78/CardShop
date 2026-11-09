using api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IConfiguration _config;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IOrderService orderService,
            IConfiguration config,
            ILogger<StripeWebhookController> logger)
        {
            _orderService = orderService;
            _config = config;
            _logger = logger;
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

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                    {
                        await _orderService.MarkOrderPaidAsync(paymentIntent.Id);
                    }
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    // DELIBERATELY does not change the order or restore stock.
                    //
                    // A declined card does not end the PaymentIntent: Stripe leaves it
                    // open (status "requires_payment_method") so the customer can try a
                    // different card on the same checkout. If we restored stock here and
                    // they then paid successfully, the order would be marked Paid with
                    // its stock already released -> oversold.
                    //
                    // Instead the order stays Pending with stock reserved. If the
                    // customer pays on a retry, payment_intent.succeeded completes it;
                    // if they give up, OrderExpirationBackgroundService cancels it and
                    // releases the stock after the expiration window.
                    //
                    // (The name used here is Stripe's real event name. The old code
                    // checked "payment_intent.failed", which Stripe never sends.)
                    if (stripeEvent.Data.Object is PaymentIntent failedIntent)
                    {
                        _logger.LogInformation(
                            "Payment attempt failed for PaymentIntent {PaymentIntentId}; order left Pending so the customer can retry.",
                            failedIntent.Id);
                    }
                }

                // Any other event type is acknowledged with 200 so Stripe doesn't retry it.
                return Ok();
            }
            catch (StripeException e)
            {
                // Most often a wrong or missing webhook signing secret (an easy slip
                // when moving from the Stripe CLI to the production webhook), or a
                // forged request. Without this line it was only a 400 in Stripe's
                // dashboard, with nothing on your side to say why.
                _logger.LogWarning(e, "Stripe webhook rejected: signature verification or payload error.");
                return BadRequest(new { error = e.Message });
            }
        }
    }
}