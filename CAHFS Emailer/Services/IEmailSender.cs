using MimeKit;

namespace CAHFS_Emailer.Services
{
    public interface IEmailSender
    {
        public Task EmailSendJob(CancellationToken cancellationToken);
        public Task<bool> SendEmail(MimeMessage message);
    }
}
