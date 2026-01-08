using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using NLog;

namespace CAHFS_Emailer.Services
{
    public class EmailService(StarLIMSContext context)
    {
        private readonly StarLIMSContext _context = context;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get the attachment data from the database. Currently supports a single attachment. May need to support multiple attachments.
        /// </summary>
        /// <param name="attachmentList">Attachment List should be the StarDocID, or multiple StarDocIDs</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<DBFileStorage?> GetAttachment(string? attachmentList)
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
                return dbFile;
            }

            return null;
        }

        /// <summary>
        /// On development and test servers, ensure emails are only sent to permitted recipients.
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void CheckEmailRecipients(MimeMessage message)
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
        /// Create a MimeMessage from an OutgoingEmail object
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public MimeMessage CreateMessage(OutgoingEmail email, byte[]? attachmentData = null)
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
