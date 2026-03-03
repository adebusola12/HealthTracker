using SendGrid;
using SendGrid.Helpers.Mail;

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
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            Console.WriteLine("API KEY: " + apiKey);
            Console.WriteLine("FROM EMAIL: " + fromEmail);

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

            var response = await client.SendEmailAsync(msg);

            Console.WriteLine("SendGrid Status Code: " + response.StatusCode);
        }
    }
}