namespace api.DTOs.Account
{
    public class EmailVerificationDto
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}
