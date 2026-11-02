using System.Text.Json;
using System.Threading.RateLimiting;
using api.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Extensions
{
    // Put this in an Extensions/ folder. Program.cs only needs two lines:
    //   builder.Services.AddAuthRateLimiting();   (with the other services)
    //   app.UseRateLimiter();                     (after UseCors, before UseAuthentication)
    public static class RateLimitingExtensions
    {
        // Policy names, referenced by [EnableRateLimiting(...)] on controller actions.
        public const string AuthAttemptsPolicy = "auth-attempts";
        public const string EmailSendingPolicy = "email-sending";

        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Login, verify-email, reset-password: 10 requests per minute per IP.
                // Generous for a real person mistyping a password, far too slow for a
                // password-guessing script. (Account lockout, in LoginAsync, covers
                // attackers who spread guesses across many IPs.)
                //
                // A SLIDING window is used instead of a fixed one so an attacker can't
                // fire 10 requests at the end of one window and 10 more at the start of the next.
                options.AddPolicy(AuthAttemptsPolicy, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientKey(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0 // reject immediately instead of queueing
                        }));

                // Register, forgot-password, send-verification: 5 per 15 minutes per IP.
                // These send EMAIL, so the concern is spamming an inbox (and your email
                // quota), not guessing. Raise this if people sharing one network (an
                // office, a school) hit it in normal use.
                options.AddPolicy(EmailSendingPolicy, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientKey(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(15),
                            SegmentsPerWindow = 5,
                            QueueLimit = 0
                        }));

                // What a rejected request gets back. Uses the same ErrorResponse shape
                // (and the same serializer, so the same property casing) as the
                // global exception middleware.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = StatusCodes.Status429TooManyRequests;
                    response.ContentType = "application/json";

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        response.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    }

                    var body = JsonSerializer.Serialize(new ErrorResponse
                    {
                        Message = "Too many requests. Please wait a moment and try again.",
                        TraceId = context.HttpContext.TraceIdentifier
                    });

                    await response.WriteAsync(body, cancellationToken);
                };
            });

            return services;
        }

        // Limits are tracked per client IP address.
        // IMPORTANT once deployed behind a proxy or load balancer (Azure App Service,
        // nginx, Cloudflare...): RemoteIpAddress becomes the PROXY's address, so every
        // user would share one bucket and be throttled together. You must also
        // configure UseForwardedHeaders so this is the real client IP. Fine locally.
        private static string GetClientKey(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}