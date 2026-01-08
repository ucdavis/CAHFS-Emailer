namespace CAHFS_Emailer.Models
{
    public class EmailWithAttachments
    {
        public OutgoingEmail Email { get; set; } = null!;
        public List<DBFileStorage> Attachments { get; set; } = [];
    }
}
