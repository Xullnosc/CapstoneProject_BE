using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["Mail"];
            var senderName = emailSettings["DisplayName"];
            var senderPassword = emailSettings["Password"];
            var host = emailSettings["Host"];
            var port = emailSettings.GetValue<int>("Port");

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                // Optionally log warning that email is not configured
                Console.WriteLine("[EmailService] Email not configured. Skipping sending.");
                return;
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlMessage;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, senderPassword);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Check your App Password or Internet. Failed to send email: {ex.Message}");
                throw; // Rethrow to let the caller know or handle it gracefully
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
