using api.DTOs.Account;
using api.Interfaces;
using api.Services;
using CardShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CardShop.Controllers
{
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;
        public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService,
            SignInManager<ApplicationUser> signInManager, EmailService emailService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _emailService = emailService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            //if (!ModelState.IsValid) { return BadRequest(ModelState); }

            //var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.Username);

            //if (user == null)
            //{
            //    return Unauthorized("Invalid username!");
            //}

            //var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            //if (!result.Succeeded) 
            //{
            //    return Unauthorized("Username not found and/or password incorrect");
            //}

            //return Ok(
            //        new NewUserDto
            //        {
            //            UserName = user.UserName,
            //            Email = user.Email,
            //            Token = _tokenService.CreateToken(user)
            //        }
            //    );

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

                // create the new token for the user
                var token = await _tokenService.CreateToken(appUser);

                // create new email verification token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);

                // create the confirmation link to send to the user
                var confirmationLink = $"{Request.Scheme}://{Request.Host}/verify-email?userId={appUser.Id}&token={Uri.EscapeDataString(token)}";

                // Send verification email via Postmark
                await _emailService.SendEmailAsync(
                    appUser.Email,
                    "Verify your email - The Bearded Troll",
                    $"<h2>Welcome to The Bearded Troll!</h2><p>Click below to verify your email:</p><a href='{confirmationLink}'>Verify Email</a>"
                );

                // Set token as secure HTTP-only cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // change to true when deployed
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("access_token", token.ToString(), cookieOptions);

                return Ok(new
                {
                    token = token,
                    UserName = appUser.UserName,
                    Email = appUser.Email
                    // Optionally remove Token from response
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

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return BadRequest("Invalid user.");

            // verifies the user's email if given a valid token
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded) return BadRequest("Invalid or expired token.");

            return Ok("Email verified successfully!");
        } // end verify email method

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Ok("If that email is registered, a reset link was sent.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            await _emailService.SendEmailAsync(
                user.Email,
                "Reset your password - The Bearded Troll",
                $"<p>Click below to reset your password:</p><a href='{resetLink}'>Reset Password</a>"
            );

            return Ok("If that email is registered, a reset link was sent.");
        } // end forgot password endpoint

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Invalid user.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Password has been reset successfully.");
        } // end reset password endpoint





    } // end controller
} // end namespace
