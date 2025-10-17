using api.DTOs.Account;
using api.Interfaces;
using api.Services;
using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace CardShop.Controllers
{
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IUserAccountService _userAccountService;
        private readonly IConfiguration _config;

        public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService,
            SignInManager<ApplicationUser> signInManager, IEmailService emailService, 
            IUserAccountService userAccountService, IConfiguration config)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _emailService = emailService;
            _userAccountService = userAccountService;
            _config = config;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            
            if (!ModelState.IsValid) { return BadRequest(ModelState); }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.Username);

            if (user == null)
            {
                return Unauthorized("Invalid username!");
            }

            // verify the user has confirmed their email
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized("Please verify your email before logging in.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized("Username not found and/or password incorrect");
            }

            var token = await _tokenService.CreateToken(user);

            // Set token as secure HTTP-only cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // change to false only in development
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("access_token", token.ToString(), cookieOptions);

            return Ok(new
            {
                token = token,
                UserName = user.UserName,
                Email = user.Email
                // Optionally remove Token from response
            });
        } // end login 


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {         
            try
            {
                // check model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // create new appuser
                var appUser = new ApplicationUser
                {
                    UserName = registerDto.Username,
                    Email = registerDto.EmailAddress,
                };

                // create the appuser in the DB
                var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);

                if (!createdUser.Succeeded)
                {
                    return StatusCode(500, createdUser.Errors);
                }

                // add user role to the new user
                var roleResult = await _userManager.AddToRoleAsync(appUser, "User");

                if (!roleResult.Succeeded)
                {
                    return StatusCode(500, roleResult.Errors);
                }

                // create new email verification token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);

                // Encode for URL
                var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

                // Use frontend base URL from configuration
                var frontendBaseUrl = _config["Frontend:BaseUrl"];
                var confirmationLink = $"{frontendBaseUrl}/verify-email?userId={appUser.Id}&token={encodedEmailToken}";

                // Send email
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

                return Ok(new
                {
                    Message = "Registration successful! Please check your email to verify your account.",
                    UserName = appUser.UserName,
                    Email = appUser.Email
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        } // end register

        [HttpPost("logout")]
        public IActionResult Logout()
        {
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
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(userEmail);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                roles
            });
        }

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
