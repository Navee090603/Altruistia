using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Altruista834OutboundMonitor.Config;
using Altruista834OutboundMonitor.Utilities;

namespace Altruista834OutboundMonitor.Services
{
    public interface IEmailService
    {
        Task SendAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken ct);
    }

    public sealed class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly ILoggingService _logger;

        public EmailService(EmailConfig config, ILoggingService logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken ct)
        {
            var recipientList = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToArray();
            if (recipientList.Length == 0)
            {
                _logger.Warn($"Email skipped (no recipients) for subject: {subject}");
                return;
            }

            await RetryHelper.RunAsync(async () =>
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_config.Sender);
                    foreach (var recipient in recipientList)
                    {
                        message.To.Add(recipient);
                    }

                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = false;

                    using (var client = new SmtpClient(_config.SmtpHost, _config.SmtpPort))
                    {
                        client.EnableSsl = _config.EnableSsl;
                        if (!string.IsNullOrWhiteSpace(_config.Username))
                        {
                            client.Credentials = new NetworkCredential(_config.Username, _config.Password);
                        }

                        await client.SendMailAsync(message).ConfigureAwait(false);
                    }
                }

                _logger.Info($"Email sent: {subject} -> {string.Join(",", recipientList)}");
            }, _config.RetryCount, TimeSpan.FromSeconds(_config.RetryDelaySeconds), (ex, attempt) =>
            {
                _logger.Warn($"Email send failed (attempt {attempt}): {ex.Message}");
            }, ct).ConfigureAwait(false);
        }
    }
}
