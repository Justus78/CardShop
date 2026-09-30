using api.Exceptions;
using System.Net;
using System.Text.Json;

namespace api.Middleware
{
    // ─────────────────────────────────────────────────────────────
    // What a "middleware" is, if you haven't written one before:
    // ASP.NET Core processes every request through a pipeline of these,
    // each one wrapping the next like layers of an onion. This one goes
    // as close to the OUTERMOST layer as possible (see Program.cs below),
    // so it wraps everything — routing, auth, your controllers, your
    // services, your repos. If an exception is thrown ANYWHERE inside
    // all of that and nothing catches it, it bubbles up through every
    // layer until it hits this one.
    //
    // This replaces the pattern you had in the original Register action:
    // a try/catch inside ONE controller action, returning a raw exception
    // object. Now every action, in every controller, gets the same safety
    // net automatically — no per-action try/catch needed anywhere.
    // ─────────────────────────────────────────────────────────────
    public class ExceptionHandlingMiddleware
    {
        // RequestDelegate = "the next thing in the pipeline." Middleware always
        // takes this in its constructor so it knows what to call after its own logic.
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        // ASP.NET Core calls this method for every single incoming request.
        // "context" is the HTTP request/response for that one call.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Let the request continue down the pipeline — into routing,
                // into your controller action, into the service, into the repo.
                // If everything succeeds, this just returns normally and the
                // rest of this method never runs.
                await _next(context);
            }
            catch (ApiException apiEx)
            {
                // One of OUR custom exceptions (NotFoundException, ConflictException, etc.)
                // — these already know their correct status code, so just use it.
                _logger.LogWarning(apiEx, "Handled API exception: {Message}", apiEx.Message);
                await WriteErrorResponseAsync(context, apiEx.StatusCode, apiEx.Message, apiEx);
            }
            catch (Exception ex)
            {
                // Anything else: a NullReferenceException, a DB connection failure,
                // a bug we didn't anticipate. This is the true "something went wrong
                // that we didn't plan for" case — always a 500, always logged loudly
                // (LogError, not LogWarning) since these are the ones worth investigating.
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await WriteErrorResponseAsync(
                    context,
                    (int)HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again.",
                    ex);
            }
        }

        private async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message, Exception ex)
        {
            // Guard against the rare case where the response has already started
            // being sent before the exception happened — you can't set headers/status
            // on a response that's already partway out the door.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, cannot write error response.");
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse
            {
                Message = message,
                TraceId = context.TraceIdentifier,
                // Only leak exception details (stack trace, exception type) when running
                // locally/in Development. A production build should never expose this —
                // it's exactly the kind of thing the original `StatusCode(500, e)` was doing
                // for every user, all the time.
                DebugDetails = _env.IsDevelopment() ? ex.ToString() : null
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
        }
    }

    // A small extension method so Program.cs can register this with clean,
    // readable syntax: app.UseExceptionHandling(); instead of the more verbose
    // app.UseMiddleware<ExceptionHandlingMiddleware>();
    // Both do exactly the same thing — this is just a naming convenience.
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}