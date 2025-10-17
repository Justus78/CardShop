using api.Interfaces;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace api.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public UserAccountService(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration config)
        {
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

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
