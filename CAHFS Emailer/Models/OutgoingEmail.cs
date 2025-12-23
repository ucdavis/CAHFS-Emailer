namespace CAHFS_Emailer.Models
{
    public class OutgoingEmail
    {
        // PK
        public int OrigRec { get; set; }

        public string? FolderNo { get; set; }

        // Core addressing
        public string? FromAddress { get; set; }
        public string? ReplyToAddress { get; set; }
        public string ToAddresses { get; set; } = null!;
        public string? CcAddresses { get; set; }
        public string? BccAddresses { get; set; }

        // Content
        public string? SubjectLine { get; set; }
        public string? BodyText { get; set; }
        public string? BodyHtml { get; set; }

        // Attachments
        public string? AttachmentList { get; set; }
        public int? AttachmentCount { get; set; }

        // Headers / options
        public string? Importance { get; set; }
        public string? Sensitivity { get; set; }
        public bool? RequestReadReceipt { get; set; }
        public bool? RequestDeliveryReceipt { get; set; }

        // Status & error tracking
        public string Status { get; set; } = "PENDING";
        public string? ErrorMessage { get; set; }

        // Timing
        public DateTime InsertedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}