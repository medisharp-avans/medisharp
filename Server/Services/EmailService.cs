using System.Net;
using System.Net.Mail;
using System.Text;
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

        /// <summary>
        /// When set, emails are written as .eml files to this folder instead of being sent (relative to the content root).
        /// The 'Tests' launch profile uses it so the end-to-end tests can read the login code.
        /// </summary>
        public string PickupDirectory { get; set; } = string.Empty;
    }

    public class EmailService(IOptions<SMTPOptions> options, IHostEnvironment env, ILogger<EmailService> logger) : IEmailService
    {
        private readonly SMTPOptions _smtp = options.Value;

        public async Task SendAsync(string to, string subject, string body)
        {
            if (!string.IsNullOrEmpty(_smtp.PickupDirectory))
            {
                await WriteToPickupDirectoryAsync(to, subject, body);
                return;
            }

            if (string.IsNullOrEmpty(_smtp.Username) || string.IsNullOrEmpty(_smtp.Password))
            {
                // Without SMTP settings, development (and the E2E tests) can still log in via the console log.
                if (!env.IsDevelopment())
                    throw new InvalidOperationException("SMTP is not configured. Set SMTP:Username and SMTP:Password.");

                logger.LogWarning("SMTP not configured, email to {To} not sent. {Subject}: {Body}", to, subject, body);
                return;
            }

            using var message = CreateMessage(_smtp.Username, to, subject, body);
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
            };
            await client.SendMailAsync(message);
        }

        private async Task WriteToPickupDirectoryAsync(string to, string subject, string body)
        {
            var directory = Path.GetFullPath(Path.Combine(env.ContentRootPath, _smtp.PickupDirectory));
            Directory.CreateDirectory(directory);

            var from = string.IsNullOrEmpty(_smtp.Username) ? "noreply@medisharp.test" : _smtp.Username;
            using var message = CreateMessage(from, to, subject, body);
            using var client = new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = directory
            };
            await client.SendMailAsync(message);
            logger.LogInformation("Email to {To} written to {Directory}", to, directory);
        }

        // Quoted-printable keeps the text (and the login code) readable in the .eml file.
        private MailMessage CreateMessage(string from, string to, string subject, string body) =>
            new(new MailAddress(from, _smtp.FromName), new MailAddress(to))
            {
                Subject = subject,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                BodyTransferEncoding = System.Net.Mime.TransferEncoding.QuotedPrintable
            };
    }
}
