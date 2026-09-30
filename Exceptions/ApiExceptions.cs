using Microsoft.AspNetCore.Http;

namespace api.Exceptions
{
    // ─────────────────────────────────────────────────────────────
    // Why these exist:
    // Your services mostly return null/bool for EXPECTED failures
    // ("user not found", "wrong password") — that's still the right
    // call for anything the controller needs to branch on.
    //
    // These exceptions are for a different category: failures that
    // are still "known" cases (not random bugs) but are awkward to
    // thread back up as a return value everywhere — e.g. deep inside
    // a repo call, or in code shared across many services. Throwing
    // one of these lets you signal "this should be a 404" (or 409,
    // etc.) from anywhere in the call stack, and the middleware below
    // translates it to the right HTTP status without any controller
    // needing a try/catch of its own.
    //
    // You don't have to adopt these everywhere today — your existing
    // null/bool pattern in CartService, UserAccountService etc. is
    // fine as-is. Reach for these when a return-value approach would
    // be awkward, or when you're adding new code later.
    // ─────────────────────────────────────────────────────────────

    // Base type so the middleware can catch "any of our custom API exceptions"
    // in one branch, and so every subclass is forced to carry a status code.
    public abstract class ApiException : Exception
    {
        public int StatusCode { get; }

        protected ApiException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    // Throw when a requested resource doesn't exist. -> 404
    public class NotFoundException : ApiException
    {
        public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound) { }
    }

    // Throw when the request conflicts with current state (e.g. duplicate,
    // already-processed order). -> 409
    public class ConflictException : ApiException
    {
        public ConflictException(string message) : base(message, StatusCodes.Status409Conflict) { }
    }

    // Throw for a request that's well-formed but violates a business rule
    // ModelState validation can't express (e.g. "quantity exceeds stock"). -> 400
    public class BusinessRuleException : ApiException
    {
        public BusinessRuleException(string message) : base(message, StatusCodes.Status400BadRequest) { }
    }
}