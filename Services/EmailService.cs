using System.Net;
using System.Net.Mail;
using api.Interfaces;

namespace api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var fromEmail = _config["Zoho:FromEmail"];
            var password = _config["Zoho:AppPassword"];
            var host = "smtp.zoho.com";
            var port = 587;

            using (var smtpClient = new SmtpClient(host, port))
            {
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(fromEmail, password);

                var mail = new MailMessage(fromEmail, to, subject, htmlBody);
                mail.IsBodyHtml = true;

                await smtpClient.SendMailAsync(mail);
            }
        }
    }
}
