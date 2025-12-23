using CAHFS_Emailer.Models;
using Microsoft.EntityFrameworkCore;

namespace CAHFS_Emailer.Data
{
    public class StarLIMSContext : DbContext
    {
        public virtual DbSet<OutgoingEmail> OrdTasks{ get; set; }

        public StarLIMSContext()
        {

        }      

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (HttpHelper.Settings != null)
            {
                optionsBuilder.UseSqlServer(HttpHelper.GetSetting<string>("ConnectionStrings", "StarLIMSDB"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutgoingEmail>(entity =>
            {
                entity.ToTable("C_Outgoing_Emails", "dbo");

                // PK / identity
                entity.HasKey(e => e.OrigRec);
                entity.Property(e => e.OrigRec)
                      .HasColumnName("ORIGREC")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.FolderNo)
                      .HasColumnName("FOLDERNO")
                      .HasMaxLength(50);

                // Core addressing
                entity.Property(e => e.FromAddress)
                      .HasColumnName("FROM_ADDRESS")
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ReplyToAddress)
                      .HasColumnName("REPLY_TO_ADDRESS")
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ToAddresses)
                      .HasColumnName("TO_ADDRESSES")
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CcAddresses)
                      .HasColumnName("CC_ADDRESSES")
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.BccAddresses)
                      .HasColumnName("BCC_ADDRESSES")
                      .HasColumnType("nvarchar(max)");

                // Content
                entity.Property(e => e.SubjectLine)
                      .HasColumnName("SUBJECT_LINE")
                      .HasMaxLength(255);

                entity.Property(e => e.BodyText)
                      .HasColumnName("BODY_TEXT")
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.BodyHtml)
                      .HasColumnName("BODY_HTML")
                      .HasColumnType("nvarchar(max)");

                // Attachments
                entity.Property(e => e.AttachmentList)
                      .HasColumnName("ATTACHMENT_LIST")
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.AttachmentCount)
                      .HasColumnName("ATTACHMENT_COUNT");

                // Headers / options
                entity.Property(e => e.Importance)
                      .HasColumnName("IMPORTANCE")
                      .HasMaxLength(20);

                entity.Property(e => e.Sensitivity)
                      .HasColumnName("SENSITIVITY")
                      .HasMaxLength(20);

                entity.Property(e => e.RequestReadReceipt)
                      .HasColumnName("REQUEST_READ_RECEIPT");

                entity.Property(e => e.RequestDeliveryReceipt)
                      .HasColumnName("REQUEST_DELIVERY_RECEIPT");

                // Status & error tracking
                entity.Property(e => e.Status)
                      .HasColumnName("STATUS")
                      .HasMaxLength(20)
                      .IsRequired()
                      .HasDefaultValue("PENDING");

                entity.Property(e => e.ErrorMessage)
                      .HasColumnName("ERROR_MESSAGE")
                      .HasColumnType("nvarchar(max)");

                // Timing
                entity.Property(e => e.InsertedAt)
                      .HasColumnName("INSERTED_AT")
                      .HasColumnType("datetime2(0)")
                      .IsRequired()
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(e => e.SentAt)
                      .HasColumnName("SENT_AT")
                      .HasColumnType("datetime2(0)");

                // Indexes
                entity.HasIndex(e => new { e.Status, e.InsertedAt })
                      .HasDatabaseName("IX_C_Outgoing_Emails_STATUS_INSERTED");

                entity.HasIndex(e => e.SentAt)
                      .HasDatabaseName("IX_C_Outgoing_Emails_SENT_AT");

                // Check constraint (matches your CK_C_Outgoing_Emails_STATUS)
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_C_Outgoing_Emails_STATUS",
                        "[STATUS] IN (N'PENDING', N'INPROCESS', N'SENT', N'ERROR')"
                    );
                });
            });

        }
    }
}
