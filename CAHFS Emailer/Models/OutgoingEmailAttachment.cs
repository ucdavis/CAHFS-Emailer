using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CAHFS_Emailer.Models
{
    public class OutgoingEmailAttachment
    {
        public int Origrec { get; set; }
        public string Origsts { get; set; } = null!;
        public string Stardocid { get; set; } = null!;
        public string AttachmentFilename { get; set; } = null!;
        public int AttachmentId { get; set; }

        public OutgoingEmail? OutgoingEmail { get; set; }
        [NotMapped]
        public DBFileStorage? FileStorage { get; set; }
    }
}
