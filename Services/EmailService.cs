using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace TurfAuthAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Turf Booking", _config["EmailSettings:SMTPUser"]));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["EmailSettings:SMTPHost"], int.Parse(_config["EmailSettings:SMTPPort"]), false);
            await client.AuthenticateAsync(_config["EmailSettings:SMTPUser"], _config["EmailSettings:SMTPPass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
