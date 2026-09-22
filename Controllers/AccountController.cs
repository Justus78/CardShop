using api.DTOs.Account;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Validating the shape of the incoming DTO (required fields, data annotations)
            if (!ModelState.IsValid) { return BadRequest(ModelState); }

            var result = await _userAccountService.LoginAsync(loginDto.Username, loginDto.Password);

            if (!result.Succeeded)
            {
                // The controller's only job here is translating "login failed" into
                // the right HTTP status (401) and the response shape your frontend expects.
                return Unauthorized(new LoginErrorDto { Error = result.ErrorMessage! });
            }

            // create the auth cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // change to false only in development
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("access_token", result.Token!, cookieOptions);

            return Ok(new
            {
                token = result.Token,
                UserName = result.UserName,
                Email = result.Email
                // Optionally remove Token from response
            });
        } // end login 

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // create the new user
            var result = await _userAccountService.RegisterAsync(registerDto.Username, registerDto.EmailAddress, registerDto.Password);

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
        } // end register

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Pure cookie manipulation — no business logic, no DB access.
            // This is a textbook example of something that belongs ENTIRELY
            // in the controller and never needed to move anywhere.
            Response.Cookies.Delete("access_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            });

            return Ok(new { message = "Logged out" });
        }

        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetAuthStatus()
        {
            // Reading claims off the current HTTP request's User principal is
            // an HTTP/auth-context concern, so it stays in the controller.
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Everything after this — looking the user up, fetching their roles —
            // is data access, so it's delegated to the service.
            var status = await _userAccountService.GetAuthStatusAsync(userEmail!);

            // Defensive check: if the JWT's email claim somehow doesn't match a real
            // user (stale token, deleted account, etc.), fail gracefully instead of
            // letting a null reference blow up into an unhandled 500.
            if (status == null) return Unauthorized();

            return Ok(status);
        }

        // ── Everything below already delegated to the service correctly
        //    and didn't need to change — included so the file is complete. ──

        [HttpPost("send-verification/{userId}")]
        public async Task<IActionResult> SendVerificationEmail(string userId)
        {
            var success = await _userAccountService.SendEmailVerificationAsync(userId);
            if (!success)
                return BadRequest(new { message = "Unable to send verification email." });

            return Ok(new { message = "Verification email sent." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDto dto)
        {
            var success = await _userAccountService.VerifyEmailAsync(dto.UserId, dto.Token);
            if (!success)
                return BadRequest(new { message = "Email verification failed." });

            return Ok(new { message = "Email verified successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var success = await _userAccountService.SendPasswordResetLinkAsync(dto.Email);
            if (!success)
                return BadRequest(new { message = "Password reset failed." });

            return Ok(new { message = "Password reset email sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _userAccountService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Password reset failed." });

            return Ok(new { message = "Password reset successfully." });
        }

    } // end controller
} // end namespace