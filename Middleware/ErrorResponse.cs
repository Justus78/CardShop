namespace api.Middleware
{
    // The shape every error response will have, no matter where it came from.
    // Your frontend can rely on this consistently instead of guessing whether
    // an error body is { message } vs { Message } vs { error } vs a raw string,
    // which is what you had before (compare Login's LoginErrorDto vs Register's
    // raw IdentityError list vs a raw exception object — three different shapes
    // for three different failure types).
    public class ErrorResponse
    {
        // Safe to show the user: "Invalid Username or Password.", "Cart item not found.", etc.
        public string Message { get; set; } = default!;

        // Useful when you add logging/monitoring later — lets you find the exact
        // log entry for a given failed request if a user reports a bug.
        public string TraceId { get; set; } = default!;

        // Only populated in Development (see middleware below). Never sent to
        // real users in Production — stack traces can leak internal details
        // (file paths, library versions, sometimes connection strings in messages).
        public string? DebugDetails { get; set; }
    }
}