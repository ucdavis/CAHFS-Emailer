

using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Hangfire.Storage.Monitoring;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Entry point for the job to send emails
        /// </summary>
        /// <returns></returns>
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
                _logger.Warn($"Exiting EmailSendJob - Status is {_emailerStatus}");
            }

            _logger.Info($"EmailSendJob finished at: {DateTime.UtcNow:HH:mm:ss}");
        }

        /// <summary>
        /// Function to send all emails in the queue. Should be locked by a semaphore to prevent duplicate emails being sent.
        /// </summary>
        /// <returns></returns>
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
                    byte[]? attachment = null;
                    if (email.AttachmentList != null)
                    {
                        attachment = await GetAttachment(email.AttachmentList);
                    }

                    var message = CreateMessage(email, attachment);

                    //check for invalid recipients in non-production environments
                    CheckEmailRecipients(message);
                    if (message.To.Count == 0)
                    {
                        _logger.Warn($"No valid recipients for email for case {email.FolderNo}.");
                        email.Status = "Error";
                        email.ErrorMessage = "No valid recipients in non-production environment.";
                        _context.OutgoingEmails.Update(email);
                        await _context.SaveChangesAsync();
                        continue;
                    }

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

        /// <summary>
        /// Get the attachment data from the database. Currently supports a single attachment. May need to support multiple attachments.
        /// </summary>
        /// <param name="attachmentList">Attachment List should be the StarDocID, or multiple StarDocIDs</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task<byte[]?> GetAttachment(string? attachmentList)
        {
            var starDocIds = attachmentList?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (starDocIds != null && starDocIds.Length >= 1)
            {
                if (starDocIds.Length > 1)
                {
                    _logger.Error($"Multiple attachments specified, but only the first will be processed: {attachmentList}");
                }
                var starDocId = starDocIds.First();
                var dbFile = await _context.DBFileStorages.FirstOrDefaultAsync(f => f.FileImageId == starDocId);
                return dbFile?.FileImage;
            }

            return null;
        }

        /// <summary>
        /// On development and test servers, ensure emails are only sent to permitted recipients.
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CheckEmailRecipients(MimeMessage message)
        {
            if (HttpHelper.Environment != null && !HttpHelper.Environment.IsProduction())
            {
                _logger.Info("Non-production environment - Checking allowed email recipients.");
                var allowedEmails = HttpHelper.GetSection<string[]>("Email:AllowedRecipients")?.ToList();
                var oldAddresses = new List<MailboxAddress>(message.To.Mailboxes);
                message.To.Clear();
                if (allowedEmails != null && allowedEmails.Count > 0)
                {
                    foreach (var address in oldAddresses)
                    {
                        if (allowedEmails.Contains(address.Address))
                        {
                            message.To.Add(address);
                        }
                        else
                        {
                            _logger.Warn($"Blocking email to {address.Address} on non-production server.");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"Blocking email to all addresses on non-production server - no allowed emails defined.");
                }
            }
        }

        /// <summary>
        /// Method to send a single email for validation purposes specified by a MimeMessage object (as opposed to an OutgoingEmail database entry).
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(MimeMessage message)
        {
            _logger.Info($"Sending manual email.");

            CheckEmailRecipients(message);
            if (message.To.Count == 0)
            {
                _logger.Warn($"No valid recipients for manual email.");
                return false;
            }

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

        /// <summary>
        /// Return true if it looks like the email was sent successfully
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool IsSuccessResult(string result)
        {
            return result.StartsWith("Ok");
        }

        /// <summary>
        /// Create a MimeMessage from an OutgoingEmail object
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private MimeMessage CreateMessage(OutgoingEmail email, byte[]? attachmentData = null)
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
            if (email.AttachmentCount > 0 && attachmentData != null)
            {
                using (var stream = new MemoryStream(attachmentData))
                {
                    bodyBuilder.Attachments.Add("FileName", stream);
                }
            }

            _logger.Debug($"Email for case {email.FolderNo}: To: {message.To.Count} addresses with {bodyBuilder.Attachments.Count} attachments.");

            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        /// <summary>
        /// Create the MimeMessage from parameters for manual email sending
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
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
