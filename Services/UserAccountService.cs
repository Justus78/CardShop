using api.DTOs.Account;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace api.Services
{
    // LOGGING RULES USED IN THIS FILE
    //  - Never log passwords, JWTs, or email/reset tokens. Never log a whole request.
    //  - Identify people by user id wherever possible (usernames only when there is no
    //    user yet, e.g. a failed registration or a login for an unknown name).
    //  - Message templates with {Placeholders}, never string interpolation, so each
    //    value stays a searchable field in Application Insights.
    //  - Levels: Information = normal events, Warning = suspicious or failed security
    //    events, Error = something broke and needs a human.
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<UserAccountService> _logger;

        // SignInManager was removed: LoginAsync now uses UserManager directly (see the
        // note there), so nothing in this class needs it any more.
        public UserAccountService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            ILogger<UserAccountService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == username);
            if (user == null)
            {
                _logger.LogInformation("Login failed: no user named {Username}.", username);
                return AuthResult.Fail("Invalid Username or Password.");
            }

            const string lockedOutMessage = "Too many failed login attempts. Please try again in a few minutes.";

            // Why UserManager and not SignInManager.CheckPasswordSignInAsync: your Identity
            // option RequireConfirmedEmail = true makes that method refuse unverified users
            // BEFORE checking the password. Doing it here lets us check the password first.
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login blocked: account {UserId} is locked out.", user.Id);
                return AuthResult.Fail(lockedOutMessage);
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("Account {UserId} was locked out after repeated failed logins.", user.Id);
                    return AuthResult.Fail(lockedOutMessage);
                }

                _logger.LogInformation("Login failed: wrong password for user {UserId}.", user.Id);
                return AuthResult.Fail("Invalid Username or Password.");
            }

            if (user.AccessFailedCount > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogInformation("Login refused: email not verified for user {UserId}.", user.Id);
                return AuthResult.Fail("Please verify your email before logging in.");
            }

            var token = await _tokenService.CreateToken(user);

            _logger.LogInformation("User {UserId} logged in.", user.Id);
            return AuthResult.Success(token.ToString(), user.UserName!, user.Email!);
        }

        public async Task<RegisterResult> RegisterAsync(string username, string email, string password)
        {
            var appUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
            };

            var createdUser = await _userManager.CreateAsync(appUser, password);
            if (!createdUser.Succeeded)
            {
                var errors = createdUser.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Registration failed for {Username}: {Errors}", username, string.Join("; ", errors));
                return RegisterResult.Fail(errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors.Select(e => e.Description).ToList();

                // Worth an Error: the account now EXISTS but has no role, so it needs a
                // human to fix. Common cause: the "User" role was never seeded in the
                // production database.
                _logger.LogError(
                    "User {UserId} was created but could not be given the 'User' role: {Errors}",
                    appUser.Id, string.Join("; ", errors));
                return RegisterResult.Fail(errors);
            }

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
            var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

            var frontendBaseUrl = _config["Frontend:BaseUrl"];
            var confirmationLink = $"{frontendBaseUrl}/verify-email?userId={appUser.Id}&token={encodedEmailToken}";

            await SendLoggedEmailAsync(
                appUser.Id,
                "registration verification",
                appUser.Email!,
                "Verify your email - The Bearded Troll",
                $@"
                <h2>Welcome to The Bearded Troll!</h2>
                <p>Thanks for signing up! Please click below to verify your email address.</p>
                <a href='{confirmationLink}' 
                   style='display:inline-block;padding:10px 20px;background-color:#4f46e5;color:#fff;text-decoration:none;border-radius:6px;'>
                   Verify Email
                </a>");

            _logger.LogInformation("User {UserId} registered; verification email sent.", appUser.Id);
            return RegisterResult.Success(appUser.UserName!, appUser.Email!);
        }

        public async Task<AuthStatusResult?> GetAuthStatusAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // A valid token whose user no longer exists: a deleted account, or a
                // signing key shared with another environment.
                _logger.LogWarning("Auth status requested with a token for an email that has no account.");
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthStatusResult
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Roles = roles
            };
        }

        public async Task<bool> SendEmailVerificationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogInformation("Verification email requested for unknown user id {UserId}.", userId);
                return false;
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var confirmationLink = $"{frontendUrl}/verify-email?userId={user.Id}&token={encodedToken}";

            await SendLoggedEmailAsync(
                user.Id,
                "verification",
                user.Email!,
                "Verify your email - The Bearded Troll",
                $"<h2>Welcome to The Bearded Troll!</h2><p>Click below to verify your email:</p><a href='{confirmationLink}'>Verify Email</a>");

            _logger.LogInformation("Verification email sent to user {UserId}.", user.Id);
            return true;
        }

        public async Task<bool> VerifyEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email verification attempted for unknown user id {UserId}.", userId);
                return false;
            }

            var decodedToken = TryDecodeToken(token);
            if (decodedToken == null)
            {
                _logger.LogWarning("Email verification for user {UserId} failed: malformed token.", userId);
                return false;
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Email verification failed for user {UserId}: {Errors}",
                    userId, string.Join("; ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("User {UserId} verified their email.", userId);
            return true;
        }

        public async Task<bool> SendPasswordResetLinkAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // The email address itself is not logged (it is personal data).
                _logger.LogInformation("Password reset requested for an email address with no account.");
                return false;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

            await SendLoggedEmailAsync(
                user.Id,
                "password reset",
                user.Email!,
                "Reset your password - The Bearded Troll",
                $"<p>Click below to reset your password:</p><a href='{resetLink}'>Reset Password</a>");

            _logger.LogInformation("Password reset email sent to user {UserId}.", user.Id);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for an email address with no account.");
                return false;
            }

            var decodedToken = TryDecodeToken(token);
            if (decodedToken == null)
            {
                _logger.LogWarning("Password reset for user {UserId} failed: malformed token.", user.Id);
                return false;
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Password reset failed for user {UserId}: {Errors}",
                    user.Id, string.Join("; ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("Password was reset for user {UserId}.", user.Id);
            return true;
        }

        // Sends an email, and if the email provider fails, records WHICH kind of email
        // and for WHICH user before rethrowing (so behavior is unchanged: the caller
        // still gets the exception). A silent email failure is one of the most common
        // problems right after deploying, because production has different email
        // settings from your machine.
        private async Task SendLoggedEmailAsync(string userId, string purpose, string to, string subject, string htmlBody)
        {
            try
            {
                await _emailService.SendEmailAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Purpose} email to user {UserId}.", purpose, userId);
                throw;
            }
        }

        // The tokens in emailed links come from the browser, so they can be truncated or
        // tampered with. Base64UrlDecode throws FormatException on bad input, which
        // used to surface as a 500. Now a bad token is just a failed verification/reset.
        private static string? TryDecodeToken(string token)
        {
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}