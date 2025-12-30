using MimeKit;

namespace CAHFS_Emailer.Services
{
    public interface IEmailSender
    {
        public MimeMessage CreateMessage(string from, string to, string subject, string body, IFormFile? attachment);
        public Task EmailSendJob();
        public Task<bool> SendEmail(MimeMessage message);
    }
}
