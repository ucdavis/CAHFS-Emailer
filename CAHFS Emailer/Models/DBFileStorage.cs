using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAHFS_Emailer.Models
{
    [Table("DB_FILE_STORAGE", Schema = "dbo")]
    public class DBFileStorage
    {
        [Column("ORIGREC")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrigRec { get; set; }

        [Column("COUNTERID")]
        [Required]
        public int CounterId { get; set; }

        [Column("FILE_IMAGE")]
        public byte[]? FileImage { get; set; }

        [Key]
        [Column("FILE_IMAGE_ID", Order = 0)]
        [Required]
        [MaxLength(20)]
        public string FileImageId { get; set; } = string.Empty;

        [Column("FILE_NAME")]
        [MaxLength(120)]
        public string? FileName { get; set; }

        [Column("ORIGSTS")]
        [Required]
        [MaxLength(1)]
        public string OrigSts { get; set; } = string.Empty;

        [Column("STORED_COMPRESS")]
        [MaxLength(2)]
        public string? StoredCompress { get; set; }

        [Column("FILE_EXTENSION")]
        [MaxLength(20)]
        public string? FileExtension { get; set; }

        [Column("DOCUMENTUMID")]
        [MaxLength(16)]
        public string? DocumentumId { get; set; }
    }
}
