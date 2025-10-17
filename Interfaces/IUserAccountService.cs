namespace api.Interfaces
{
    public interface IUserAccountService
    {
        Task<bool> SendEmailVerificationAsync(string userId);
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<bool> SendPasswordResetLinkAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
