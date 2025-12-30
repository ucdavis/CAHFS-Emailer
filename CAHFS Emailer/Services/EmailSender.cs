

using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Hangfire.Storage.Monitoring;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using NLog;

namespace CAHFS_Emailer.Services
{

    public class EmailSender(StarLIMSContext context, IOptions<SMTPSettings> smtpSettings) : IEmailSender
    {
        private const EmailerStatus available = EmailerStatus.Available;

        //semaphore to ensure multiple jobs don't run at the same time
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        public enum EmailerStatus { Available, Checking, Sending, Finishing }
        private static EmailerStatus _emailerStatus = available;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly StarLIMSContext _context = context;
        private readonly SMTPSettings _smtpSettings = smtpSettings.Value; // HttpHelper.GetSetting<SMTPSettings>("Config", "AmazonSES")
                 // ?? throw new InvalidOperationException("Amazon SES settings are not configured properly.");

        public async Task EmailSendJob()
        {
            _logger.Info($"EmailSendJob started at: {DateTime.UtcNow:HH:mm:ss}");

            if (_semaphore.Wait(0))
            {
                _emailerStatus = EmailerStatus.Checking;
                await SendEmailsAsync();
            }
            else
            {
                _logger.Warn($"Exiting EmailSendkJob - Status is {_emailerStatus}");
            }
        }

        private async Task SendEmailsAsync()
        {
            //get emails from the database
            var emails = _context.OutgoingEmails.Where(e => e.Status == "Pending").ToList();

            _logger.Info($"Sending {emails.Count} emails.");

            //connect to SES
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

            _logger.Info($"Connected to SMTP server.");

            foreach (var email in emails)
            {
                //send email logic here
                _logger.Info($"Sending email for case {email.FolderNo} to: {email.ToAddresses}. Emails contains {email.AttachmentCount} attachments.");

                try
                {
                    var message = CreateMessage(email);
                    var result = await client.SendAsync(message);
                    if (IsSuccessResult(result))
                    {
                        _logger.Info($"Email sent successfully for case {email.FolderNo}. Result: {result}");
                        email.Status = "Sent";
                    }
                    else
                    {
                        _logger.Error($"Email failed to send for case {email.FolderNo}. Result: {result}");
                        email.Status = "Error";
                        email.ErrorMessage = $"Failed to send email. Result: {result}";
                    }
                    _context.OutgoingEmails.Update(email);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error sending email message for case {email.FolderNo}. {ex.Message}");
                    continue;
                }
            }

            // Disconnect from the server
            await client.DisconnectAsync(true);
        }

        public async Task<bool> SendEmail(MimeMessage message)
        {
            _logger.Info($"Sending manual email.");

            //connect to SES
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

            _logger.Info($"Connected to SMTP server.");
            try
            {
                var result = await client.SendAsync(message);
                if (IsSuccessResult(result))
                {
                    _logger.Info($"Manual email sent successfully. Result: {result}");
                    return true;
                }
                else
                {
                    _logger.Error($"Manual email failed to send for. Result: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error sending manual email. {ex.Message}");
            }

            return false;
        }

        private static bool IsSuccessResult(string result)
        {
            return result.StartsWith("Ok");
        }

        private static MimeMessage CreateMessage(OutgoingEmail email)
        {
            //create message with from, to, subject
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", email.FromAddress));
            message.To.Add(new MailboxAddress("", email.ToAddresses));
            message.Subject = email.SubjectLine;

            var bodyBuilder = new BodyBuilder();

            //create body with html and/or text
            if (!string.IsNullOrEmpty(email.BodyHtml))
            {
                bodyBuilder.HtmlBody = email.BodyHtml;
            }
            if (!string.IsNullOrWhiteSpace(email.BodyText))
            {
                bodyBuilder.TextBody = email.BodyText;
            }

            //builder.Attachments.Add(fileName, attachmentData, ContentType.Parse("application/pdf"));

            //add attachments
            if (email.AttachmentCount > 0 && email.AttachmentData != null)
            {
                using (var stream = new MemoryStream(email.AttachmentData))
                {
                    bodyBuilder.Attachments.Add("FileName", stream);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        public MimeMessage CreateMessage(string from, string to, string subject, string body, IFormFile? attachment)
        {
            //create message with from, to, subject
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", from));
            message.To.Add(new MailboxAddress("", to));
            message.Bcc.Add(new MailboxAddress("", "bsedwards@ucdavis.edu"));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = body;

            //builder.Attachments.Add(fileName, attachmentData, ContentType.Parse("application/pdf"));

            //add attachment
            if (attachment != null)
            {
                using (var stream = attachment.OpenReadStream())
                {
                    bodyBuilder.Attachments.Add("Attachment.pdf", stream);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }
    }
}
