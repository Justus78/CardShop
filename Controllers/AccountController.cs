using api.DTOs.Account;
using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace CardShop.Controllers
{
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
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

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized("Username not found and/or password incorrect");
            }

            var token = _tokenService.CreateToken(user);

            // Set token as secure HTTP-only cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // change to false only in development
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("access_token", token, cookieOptions);

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
            //try
            //{
            //    if (!ModelState.IsValid)
            //    {
            //        return BadRequest(ModelState);
            //    }

            //    var appUser = new ApplicationUser
            //    {
            //        UserName = registerDto.Username,
            //        Email = registerDto.EmailAddress,
            //    };

            //    var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);

            //    if (createdUser.Succeeded) 
            //    {
            //        var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            //        if (roleResult.Succeeded)
            //        {
            //            return Ok(
            //                new NewUserDto
            //                {
            //                    UserName = appUser.UserName,
            //                    Email = appUser.Email,
            //                    Token = _tokenService.CreateToken(appUser)
            //                }
            //            );
            //        } else
            //        {
            //            return StatusCode(500, roleResult.Errors);
            //        } // end if else
            //    } 
            //    else
            //    {
            //        return StatusCode(500, createdUser.Errors);
            //    } // end if else for created user
            //}
            //catch (Exception e) 
            //{
            //    return StatusCode(500, e);
            //} // end try catch

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var appUser = new ApplicationUser
                {
                    UserName = registerDto.Username,
                    Email = registerDto.EmailAddress,
                };

                var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);

                if (!createdUser.Succeeded)
                {
                    return StatusCode(500, createdUser.Errors);
                }

                var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                if (!roleResult.Succeeded)
                {
                    return StatusCode(500, roleResult.Errors);
                }

                var token = _tokenService.CreateToken(appUser);

                // 👇 Set token as secure HTTP-only cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // change to true when deployed
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("access_token", token, cookieOptions);

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


    } // end controller
} // end namespace
