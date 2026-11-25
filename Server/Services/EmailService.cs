using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Server.Services.InterFaces;

namespace Server.Services
{
    /// <summary>
    /// SMTP settings from the "Smtp" configuration section (one Purelymail account).
    /// Set the password via user-secrets locally or the <c>Smtp__Password</c> environment variable, never in appsettings.json.
    /// </summary>
    public class SMTPOptions 
    {
        public const string SectionName = "SMTP";

        public string Host { get; set; } = "smtp.purelymail.com";

        /// <summary>587 = STARTTLS. Port 465 (implicit TLS) is not supported by <see cref="SmtpClient"/>.</summary>
        public int Port { get; set; } = 587;

        /// <summary>The full Purelymail address, also used as sender.</summary>
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromName { get; set; } = "MediSharp";
    }

    public class EmailService(IOptions<SMTPOptions> options, IHostEnvironment env, ILogger<EmailService> logger) : IEmailService
    {
        private readonly SMTPOptions _smtp = options.Value;

        public async Task SendAsync(string to, string subject, string body)
        {
            if (string.IsNullOrEmpty(_smtp.Username) || string.IsNullOrEmpty(_smtp.Password))
            {
                // Without SMTP settings, development (and the E2E tests) can still log in via the console log.
                if (!env.IsDevelopment())
                    throw new InvalidOperationException("SMTP is not configured. Set SMTP:Username and SMTP:Password.");

                logger.LogWarning("SMTP not configured, email to {To} not sent. {Subject}: {Body}", to, subject, body);
                return;
            }

            using var message = new MailMessage(new MailAddress(_smtp.Username, _smtp.FromName), new MailAddress(to))
            {
                Subject = subject,
                Body = body
            };
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
            };
            await client.SendMailAsync(message);
        }
    }
}
