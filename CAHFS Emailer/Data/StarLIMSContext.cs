using CAHFS_Emailer.Models;
using Microsoft.EntityFrameworkCore;

namespace CAHFS_Emailer.Data
{
    public class StarLIMSContext : DbContext
    {
        public virtual DbSet<OutgoingEmail> OutgoingEmails{ get; set; }
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
