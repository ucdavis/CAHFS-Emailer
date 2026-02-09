using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAHFS_Emailer.Models
{
    [Table("C_Outgoing_Emails", Schema = "dbo")]
    public class OutgoingEmail
    {
        [Key]
        [Column("ORIGREC")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrigRec { get; set; }

        [Column("FOLDERNO")]
        [MaxLength(50)]
        public string? FolderNo { get; set; }

        [Column("FROM_ADDRESS")]
        public string? FromAddress { get; set; }

        [Column("REPLY_TO_ADDRESS")]
        public string? ReplyToAddress { get; set; }

        [Column("TO_ADDRESSES")]
        [Required]
        public string ToAddresses { get; set; } = string.Empty;

        [Column("CC_ADDRESSES")]
        public string? CcAddresses { get; set; }

        [Column("BCC_ADDRESSES")]
        public string? BccAddresses { get; set; }

        [Column("SUBJECT_LINE")]
        [MaxLength(255)]
        public string? SubjectLine { get; set; }

        [Column("BODY_TEXT")]
        public string? BodyText { get; set; }

        [Column("BODY_HTML")]
        public string? BodyHtml { get; set; }

        [Column("ATTACHMENT_ID")]
        public int? AttachmentId { get; set; }

        [Column("ATTACHMENT_COUNT")]
        public int? AttachmentCount { get; set; }

        [Column("IMPORTANCE")]
        [MaxLength(20)]
        public string? Importance { get; set; }

        [Column("SENSITIVITY")]
        [MaxLength(20)]
        public string? Sensitivity { get; set; }

        [Column("REQUEST_READ_RECEIPT")]
        public bool? RequestReadReceipt { get; set; }

        [Column("REQUEST_DELIVERY_RECEIPT")]
        public bool? RequestDeliveryReceipt { get; set; }

        [Column("STATUS")]
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [Column("ERROR_MESSAGE")]
        public string? ErrorMessage { get; set; }

        [Column("INSERTED_AT")]
        [Required]
        public DateTime InsertedAt { get; set; }

        [Column("SENT_AT")]
        public DateTime? SentAt { get; set; }

        public OutgoingEmailAttachment? Attachment { get; set; }
    }
}