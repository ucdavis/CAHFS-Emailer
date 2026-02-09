using CAHFS_Emailer.Models;
using Microsoft.EntityFrameworkCore;

namespace CAHFS_Emailer.Data
{
    public class StarLIMSContext : DbContext
    {
        public virtual DbSet<OutgoingEmail> OutgoingEmails{ get; set; }
        public virtual DbSet<OutgoingEmailAttachment> OutgoingEmailAttachments { get; set; }
        public DbSet<DBFileStorage> DBFileStorages { get; set; }
        public DbSet<FolderLatestCaseReport> FolderLatestCaseReports { get; set; }

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
            // Configure OutgoingEmail entity
            modelBuilder.Entity<OutgoingEmail>(entity =>
            {
                entity.HasKey(e => e.OrigRec);

                entity.Property(e => e.OrigRec)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.InsertedAt)
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.SentAt)
                    .HasColumnType("datetime2(0)");

                entity.HasOne(e => e.Attachment)
                    .WithOne(a => a.OutgoingEmail)
                    .HasForeignKey<OutgoingEmail>(e => e.AttachmentId);

                entity.ToTable(tb => tb.UseSqlOutputClause(false));
            });

            modelBuilder.Entity<OutgoingEmailAttachment>(entity =>
            {
                entity.ToTable("C_OUTGOING_EMAIL_ATTACHMENTS", "dbo");

                entity.HasKey(e => e.AttachmentId);

                entity.Property(e => e.Origrec)
                    .HasColumnName("ORIGREC")
                    .IsRequired();

                entity.Property(e => e.Origsts)
                    .HasColumnName("ORIGSTS")
                    .HasColumnType("nchar(1)")
                    .HasMaxLength(1)
                    .IsRequired();

                entity.Property(e => e.Stardocid)
                    .HasColumnName("STARDOCID")
                    .HasColumnType("nvarchar(20)")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.AttachmentFilename)
                    .HasColumnName("ATTACHMENT_FILENAME")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.AttachmentId)
                    .HasColumnName("ATTACHMENT_ID")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn(1, 1);
                
                entity.Ignore(e => e.FileStorage);
            });

            // Configure DbFileStorage entity with composite key
            modelBuilder.Entity<DBFileStorage>(entity =>
            {
                entity.HasKey(e => new { e.FileImageId, e.CounterId })
                    .HasName("DB_FILE_STORAGE_PK");

                entity.Property(e => e.OrigRec)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn(92873, 1);

                entity.Property(e => e.OrigSts)
                    .IsFixedLength()
                    .HasMaxLength(1);

                entity.Property(e => e.FileImage)
                       .HasColumnName("FILE_IMAGE")
                       .IsRequired();

                entity.Property(e => e.FileImageId)
                    .HasColumnName("FILE_IMAGE_ID")
                    .IsRequired();

                entity.Property(e => e.FileName)
                    .HasColumnName("FILE_NAME")
                    .HasMaxLength(20);

                entity.Property(e => e.StoredCompress)
                    .HasColumnName("STORED_COMPRESS");

                entity.Property(e => e.FileExtension)
                    .HasColumnName("FILE_EXTENSION");
            });

            //latest report view
            modelBuilder.Entity<FolderLatestCaseReport>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("C_FOLDER_LATEST_CASE_RPT_IMAGE_V");
            });
        }
    }
}
