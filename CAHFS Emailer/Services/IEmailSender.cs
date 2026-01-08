using MimeKit;

namespace CAHFS_Emailer.Services
{
    public interface IEmailSender
    {
        public Task EmailSendJob();
        public Task<bool> SendEmail(MimeMessage message);
    }
}
