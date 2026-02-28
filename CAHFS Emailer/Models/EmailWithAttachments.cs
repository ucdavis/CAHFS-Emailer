namespace CAHFS_Emailer.Models
{
    public class EmailWithAttachments
    {
        public OutgoingEmail Email { get; set; } = null!;
        public List<OutgoingEmailAttachment> Attachments { get; set; } = [];
    }
}
