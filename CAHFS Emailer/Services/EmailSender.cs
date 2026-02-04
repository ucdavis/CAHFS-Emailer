

using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Hangfire.Storage.Monitoring;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using MimeKit.Text;
using NLog;

namespace CAHFS_Emailer.Services
{

    public class EmailSender(StarLIMSContext context, IOptions<SMTPSettings> smtpSettings, EmailService emailService) : IEmailSender
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
        private readonly EmailService _emailService = emailService;

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
                try
                {
                    await SendEmailsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error in EmailSendJob. {ex.ToString()}");
                }
                _emailerStatus = EmailerStatus.Available;
                _semaphore.Release();
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

            List<EmailWithAttachments> emailsWithAttachments = await GetEmailsWithAttachments(emails);            

            //connect to SES
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error connecting to SMTP server. {ex.Message} Server is {_smtpSettings.Server}:{_smtpSettings.Port}");
                return;
            }

            _logger.Info($"Connected to SMTP server.");
            _emailerStatus = EmailerStatus.Sending;

            foreach (var email in emailsWithAttachments)
            {
                var e = email.Email;
                _logger.Info($"Sending email for case {e.FolderNo} to: {e.ToAddresses}. Emails contains {e.AttachmentCount} attachments.");

                try
                {
                    var message = _emailService.CreateMessage(e, email.Attachments.FirstOrDefault());

                    //check for invalid recipients in non-production environments
                    _emailService.CheckEmailRecipients(message);
                    if (message.To.Count == 0)
                    {
                        _logger.Warn($"No valid recipients for email for case {e.FolderNo}.");
                        e.Status = "Error";
                        e.ErrorMessage = "No valid recipients in non-production environment.";
                        _context.OutgoingEmails.Update(e);
                        await _context.SaveChangesAsync();
                        continue;
                    }

                    var result = await client.SendAsync(message);
                    if (IsSuccessResult(result))
                    {
                        _logger.Info($"Email sent successfully for case {e.FolderNo}. Result: {result}");
                        e.Status = "Sent";
                    }
                    else
                    {
                        _logger.Error($"Email failed to send for case {e.FolderNo}. Result: {result}");
                        e.Status = "Error";
                        e.ErrorMessage = $"Failed to send email. Result: {result}";
                    }
                    _context.OutgoingEmails.Update(e);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error sending email message for case {e.FolderNo}. {ex.Message}");
                    continue;
                }
            }

            _emailerStatus = EmailerStatus.Finishing;

            // Disconnect from the server
            await client.DisconnectAsync(true);
        }

        private async Task<List<EmailWithAttachments>> GetEmailsWithAttachments(List<OutgoingEmail> emails)
        {
            var emailsWithAttachments = new List<EmailWithAttachments>();

            //get attachments for each email, if they exist
            foreach (var email in emails)
            {
                var emailWithAttachments = new EmailWithAttachments()
                {
                    Email = email,
                    Attachments = new List<OutgoingEmailAttachment>()
                };

                if (email.AttachmentList != null)
                {
                    var attachment = await _emailService.GetAttachment(email.AttachmentList);
                    if (attachment != null)
                    {
                        emailWithAttachments.Attachments.Add(attachment);
                    }
                }

                emailsWithAttachments.Add(emailWithAttachments);
            }

            return emailsWithAttachments;
        }


        /// <summary>
        /// Method to send a single email for validation purposes specified by a MimeMessage object (as opposed to an OutgoingEmail database entry).
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(MimeMessage message)
        {
            _logger.Info($"Sending manual email.");

            _emailService.CheckEmailRecipients(message);
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
    }
}
