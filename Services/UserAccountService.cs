using api.DTOs.Account;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace api.Services
{
    // This is where the actual "what does it mean to log in / register /
    // check status" logic lives now. Note there's still no raw ApplicationDbContext
    // here — for Identity-related features, UserManager and SignInManager
    // ARE your data-access layer (Identity wraps EF Core internally), so we
    // don't need a separate custom repository the way you did for Cart/Orders/Products.
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // NEW — needed for password verification
        private readonly ITokenService _tokenService;                  // NEW — needed to mint the JWT
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public UserAccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────
        // LOGIN
        // All the "is this a valid login" logic that used to live directly
        // in the controller's [HttpPost("login")] action. Every early return
        // here used to be an `Unauthorized(...)` inside the controller —
        // now it's just "return a failed result and let the controller
        // decide how to phrase the HTTP response."
        // ─────────────────────────────────────────────────────────
        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            // Step 1: does a user with this username even exist?
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == username);
            if (user == null)
            {
                // Deliberately vague message (don't reveal whether it was the
                // username or password that was wrong — that's a security best practice,
                // preserved exactly from your original code).
                return AuthResult.Fail("Invalid Username or Password.");
            }

            // Step 2: have they verified their email? (business rule — no verified
            // email, no login, regardless of whether the password is right)
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return AuthResult.Fail("Please verify your email before logging in.");
            }

            // Step 3: is the password actually correct?
            // `false` = don't lock the account out after failed attempts (matches your original call).
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded)
            {
                return AuthResult.Fail("Invalid Username or Password.");
            }

            // Step 4: all checks passed — mint a token and hand back a success result.
            // NOTE: we do NOT set the cookie here. Writing to Response.Cookies is an
            // HTTP-response concern, not a business-logic concern, so that stays in
            // the controller. The service's job ends at "here is a valid token."
            var token = await _tokenService.CreateToken(user);
            return AuthResult.Success(token.ToString(), user.UserName!, user.Email!);
        }

        // ─────────────────────────────────────────────────────────
        // REGISTER
        // Same idea: create the user, assign a role, send the verification email.
        // All pure business/domain logic, zero HTTP awareness.
        // ─────────────────────────────────────────────────────────
        public async Task<RegisterResult> RegisterAsync(string username, string email, string password)
        {
            var appUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
            };

            // Identity handles password hashing + uniqueness checks internally here.
            var createdUser = await _userManager.CreateAsync(appUser, password);
            if (!createdUser.Succeeded)
            {
                // .Description turns Identity's IdentityError objects into plain strings
                // so this service's return type doesn't force the controller (or anything
                // else calling this service) to reference Identity-specific types.
                return RegisterResult.Fail(createdUser.Errors.Select(e => e.Description));
            }

            // Every new user gets the "User" role by default — a business rule,
            // which is exactly the kind of thing that belongs in the service, not the controller.
            var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            if (!roleResult.Succeeded)
            {
                return RegisterResult.Fail(roleResult.Errors.Select(e => e.Description));
            }

            // Generate a one-time email confirmation token and URL-encode it
            // (raw tokens can contain characters that break URLs).
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
            var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

            var frontendBaseUrl = _config["Frontend:BaseUrl"];
            var confirmationLink = $"{frontendBaseUrl}/verify-email?userId={appUser.Id}&token={encodedEmailToken}";

            // Sending the email is a side-effect of "registering," so it lives here
            // rather than in the controller — the controller shouldn't need to know
            // that registration also triggers an email.
            await _emailService.SendEmailAsync(
                appUser.Email,
                "Verify your email - The Bearded Troll",
                $@"
                <h2>Welcome to The Bearded Troll!</h2>
                <p>Thanks for signing up! Please click below to verify your email address.</p>
                <a href='{confirmationLink}' 
                   style='display:inline-block;padding:10px 20px;background-color:#4f46e5;color:#fff;text-decoration:none;border-radius:6px;'>
                   Verify Email
                </a>"
            );

            return RegisterResult.Success(appUser.UserName!, appUser.Email!);
        }

        // ─────────────────────────────────────────────────────────
        // AUTH STATUS
        // Looks up the currently-authenticated user's info + roles.
        // Returns null (rather than throwing) if somehow the email on the
        // JWT claim doesn't match a real user — the controller turns that
        // into a 401 rather than crashing with a NullReferenceException,
        // which is what the original code was at risk of doing.
        // ─────────────────────────────────────────────────────────
        public async Task<AuthStatusResult?> GetAuthStatusAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthStatusResult
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Roles = roles
            };
        }

        // ─────────────────────────────────────────────────────────
        // Everything below this line already existed and is UNCHANGED —
        // included so the file is complete and you can see the whole class at once.
        // ─────────────────────────────────────────────────────────

        public async Task<bool> SendEmailVerificationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var confirmationLink = $"{frontendUrl}/verify-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailAsync(
                user.Email,
                "Verify your email - The Bearded Troll",
                $"<h2>Welcome to The Bearded Troll!</h2><p>Click below to verify your email:</p><a href='{confirmationLink}'>Verify Email</a>"
            );

            return true;
        }

        public async Task<bool> VerifyEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            return result.Succeeded;
        }

        public async Task<bool> SendPasswordResetLinkAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={encodedToken}";

            await _emailService.SendEmailAsync(
                user.Email,
                "Reset your password - The Bearded Troll",
                $"<p>Click below to reset your password:</p><a href='{resetLink}'>Reset Password</a>"
            );

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            return result.Succeeded;
        }
    }
}