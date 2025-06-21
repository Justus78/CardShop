using Microsoft.AspNetCore.Mvc;
using Stripe;
using CardShop.Data;


public class WebHookController
{
    [ApiController]
    [Route("api/webhook/stripe")]
    public class WebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebhookController> _logger;
        private readonly string _webhookSecret;

        public WebhookController(ApplicationDbContext context, ILogger<WebhookController> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _webhookSecret = config["Stripe:WebhookSecret"];
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Event stripeEvent;

            try
            {
                var signatureHeader = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _webhookSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError($"⚠️ Webhook signature verification failed: {ex.Message}");
                return BadRequest();
            }

            // Log or handle specific event types
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                var paymentIntentId = paymentIntent.Id;

                var order = _context.Orders.FirstOrDefault(o => o.TransactionId == paymentIntentId);
                if (order != null)
                {
                    order.PaymentStatus = "Paid";
                    await _context.SaveChangesAsync();
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                var paymentIntentId = paymentIntent.Id;

                var order = _context.Orders.FirstOrDefault(o => o.TransactionId == paymentIntentId);
                if (order != null)
                {
                    order.PaymentStatus = "Failed";
                    await _context.SaveChangesAsync();
                }
            }


            return Ok();
        }
    }
}

