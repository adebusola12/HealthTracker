using System.Net;
using System.Net.Mail;

namespace HealthTracker.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpClient = new SmtpClient("smtp-relay.brevo.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    "a62c66001@smtp-brevo.com",
                    _configuration["EmailSettings:SmtpPassword"]
                ),
                EnableSsl = true,
                Timeout = 10000
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("adebanjoadebusola12@gmail.com", "HealthTracker"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}