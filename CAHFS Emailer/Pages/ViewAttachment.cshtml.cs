using CAHFS_Emailer.Data;
using CAHFS_Emailer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CAHFS_Emailer.Pages
{

    public class ViewAttachmentModel(StarLIMSContext context, EmailService emailService) : PageModel
    {
        private readonly StarLIMSContext _context = context;
        private readonly EmailService _emailService = emailService;
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var attachment = await _emailService.GetAttachment((int)id);
            
            if (attachment == null || attachment?.FileStorage?.FileImage == null)
            {
                return NotFound();
            }

            return File(attachment.FileStorage.FileImage, GetMimeType(attachment.FileStorage.FileExtension?.ToUpper()), attachment.AttachmentFilename);
        }

        private string GetMimeType(string? extension)
        {
            return extension switch
            {
                "AVI" => "video/x-msvideo",
                "BMP" => "image/bmp",
                "CSV" => "text/csv",
                "DOC" => "application/msword",
                "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "EXE" => "application/vnd.microsoft.portable-executable",
                "GIF" => "image/gif",
                "HTM" => "text/html",
                "JPEG" => "image/jpeg",
                "JPF" => "image/jpx",
                "JPG" => "image/jpeg",
                "MSG" => "application/vnd.ms-outlook",
                "PDF" => "application/pdf",
                "PNG" => "image/png",
                "PPT" => "application/vnd.ms-powerpoint",
                "PPTX" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "RTF" => "application/rtf",
                "TIF" => "image/tiff",
                "TIFF" => "image/tiff",
                "TXT" => "text/plain",
                "XLS" => "application/vnd.ms-excel",
                "XLSM" => "application/vnd.ms-excel.sheet.macroEnabled.12",
                "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "XML" => "application/xml",
                "XPS" => "application/vnd.ms-xpsdocument",
                "ZIP" => "application/zip",
                _ => "application/octet-stream" // Default for unknown types
            };
        }
    }
}
