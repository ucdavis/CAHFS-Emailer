using System.ComponentModel.DataAnnotations.Schema;

namespace CAHFS_Emailer.Models
{
    public class FolderLatestCaseReport
    {
        [Column("folderno")]
        public string FolderNo { get; set; } = string.Empty;
        [Column("Latest_Report")]
        public byte[]? LatestReport { get; set; }

    }
}
