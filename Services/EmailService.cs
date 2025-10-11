using api.Interfaces;
using Microsoft.IdentityModel.Tokens;
using PostmarkDotNet;

namespace api.Services
{
    public class EmailService : IEmailService
    {
        // private variables
        private readonly string _apiKey;
        private readonly string _fromEmail;

        private EmailService(IConfiguration _config)
        {
            _apiKey = _config["Postmark:ApiKey"];
            _fromEmail = _config["Postmark:FromEmail"];
        }
        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var client = new PostmarkClient(_apiKey); // create the postmark client with api key


            // create the message to send
            var message = new PostmarkMessage
            {
                From = _fromEmail,
                To = to,
                Subject = subject,
                HtmlBody = htmlBody,
                MessageStream = "outbound"
            };

            await client.SendMessageAsync(message); // send the email
        }
    }
}
