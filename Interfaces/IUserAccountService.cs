using api.DTOs.Account;

namespace api.Interfaces
{
    public interface IUserAccountService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task<RegisterResult> RegisterAsync(string username, string email, string password);
        Task<AuthStatusResult?> GetAuthStatusAsync(string email);
        Task<bool> SendEmailVerificationAsync(string userId);
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<bool> SendPasswordResetLinkAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}