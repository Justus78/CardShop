namespace api.DTOs.Account
{

    public class AuthResult
    {
        // True only when login fully succeeded (user found, email verified, password correct).
        public bool Succeeded { get; set; }

        // Human-readable reason for failure. Null when Succeeded == true.
        // The controller just forwards this straight into LoginErrorDto —
        // it doesn't need to know *why* login failed, just what message to show.
        public string? ErrorMessage { get; set; }

        // Everything below is only populated on success.
        public string? Token { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        // Small factory helpers so the service doesn't have to write
        // "new AuthResult { Succeeded = false, ... }" over and over.
        public static AuthResult Fail(string error) => new() { Succeeded = false, ErrorMessage = error };

        public static AuthResult Success(string token, string userName, string email) => new()
        {
            Succeeded = true,
            Token = token,
            UserName = userName,
            Email = email
        };
    }

    public class RegisterResult
    {
        public bool Succeeded { get; set; }

        // Identity gives back a list of IdentityError objects (password too short,
        // username taken, etc). We flatten them to plain strings here so the
        // service layer doesn't leak an Identity-specific type up to the controller —
        // the controller only knows "here are some error strings to show the user."
        public IEnumerable<string> Errors { get; set; } = [];

        public string? UserName { get; set; }
        public string? Email { get; set; }

        public static RegisterResult Fail(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };

        public static RegisterResult Success(string userName, string email) => new()
        {
            Succeeded = true,
            UserName = userName,
            Email = email
        };
    }

    public class AuthStatusResult
    {
        // Returned by GetAuthStatusAsync. Nullable at the call site (see service)
        // so the controller can tell "user not found" apart from "user found, here's their data"
        // without a second bool flag.
        public string Id { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public IList<string> Roles { get; set; } = [];
    }
}