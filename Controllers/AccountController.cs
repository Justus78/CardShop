using api.DTOs.Account;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace api.Controllers
{
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public AccountController(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        // One place that defines the auth cookie's flags. Login and Logout must use
        // IDENTICAL flags (except Expires) or the browser won't delete the cookie.
        // Secure + SameSite=None is what a cross-site frontend needs. See the
        // SameSite note in the walkthrough: if your frontend and API share a
        // registrable domain, Lax is safer.
        private static CookieOptions BuildAuthCookieOptions(DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expires
            };
        }

        [EnableRateLimiting(RateLimitingExtensions.AuthAttemptsPolicy)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid) { return BadRequest(ModelState); }

            var result = await _userAccountService.LoginAsync(loginDto.Username, loginDto.Password);

            if (!result.Succeeded)
            {
                return Unauthorized(new LoginErrorDto { Error = result.ErrorMessage! });
            }

            Response.Cookies.Append("access_token", result.Token!,
                BuildAuthCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

            return Ok(new
            {
                UserName = result.UserName,
                Email = result.Email
            });
        }

        [EnableRateLimiting(RateLimitingExtensions.EmailSendingPolicy)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userAccountService.RegisterAsync(
                registerDto.Username, registerDto.EmailAddress, registerDto.Password);

            if (!result.Succeeded)
            {
                return StatusCode(500, result.Errors);
            }

            return Ok(new
            {
                Message = "Registration successful! Please check your email to verify your account.",
                result.UserName,
                result.Email
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token", BuildAuthCookieOptions());
            return Ok(new { message = "Logged out" });
        }

        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetAuthStatus()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var status = await _userAccountService.GetAuthStatusAsync(userEmail!);

            if (status == null) return Unauthorized();

            return Ok(status);
        }

        [EnableRateLimiting(RateLimitingExtensions.EmailSendingPolicy)]
        [HttpPost("send-verification/{userId}")]
        public async Task<IActionResult> SendVerificationEmail(string userId)
        {
            var success = await _userAccountService.SendEmailVerificationAsync(userId);
            if (!success)
                return BadRequest(new { message = "Unable to send verification email." });

            return Ok(new { message = "Verification email sent." });
        }

        [EnableRateLimiting(RateLimitingExtensions.AuthAttemptsPolicy)]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDto dto)
        {
            var success = await _userAccountService.VerifyEmailAsync(dto.UserId, dto.Token);
            if (!success)
                return BadRequest(new { message = "Email verification failed." });

            return Ok(new { message = "Email verified successfully." });
        }

        [EnableRateLimiting(RateLimitingExtensions.EmailSendingPolicy)]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            // The result is deliberately ignored. Answering "that email doesn't exist"
            // (400) versus "email sent" (200) would let anyone check which email
            // addresses have accounts here. Every request now gets the same answer.
            await _userAccountService.SendPasswordResetLinkAsync(dto.Email);

            return Ok(new { message = "If an account exists for that email, a password reset link has been sent." });
        }

        [EnableRateLimiting(RateLimitingExtensions.AuthAttemptsPolicy)]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _userAccountService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Password reset failed." });

            return Ok(new { message = "Password reset successfully." });
        }
    }
}