using System.Net;
using System.Net.Mail;
using api.Interfaces;

namespace api.Services
{
    public class EmailService : IEmailService
    {
        private const string SmtpHost = "smtp.zoho.com";
        private const int SmtpPort = 587;

        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var fromEmail = _config["Zoho:FromEmail"];
            var password = _config["Zoho:AppPassword"];

            // Without this check, a missing setting surfaces as an ArgumentNullException
            // from inside MailMessage, which says nothing about WHICH setting is missing.
            // In Azure the app settings are named Zoho__FromEmail and Zoho__AppPassword
            // (a double underscore stands in for the colon).
            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Email is not configured. Set 'Zoho:FromEmail' and 'Zoho:AppPassword' " +
                    "(in Azure: app settings 'Zoho__FromEmail' and 'Zoho__AppPassword').");
            }

            using var smtpClient = new SmtpClient(SmtpHost, SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            // MailMessage is disposable too; the original never disposed it.
            using var mail = new MailMessage(fromEmail, to, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(mail);
        }
    }
}