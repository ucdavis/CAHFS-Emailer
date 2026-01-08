using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using Polly;
using System.Net.Mail;

namespace CAHFS_Emailer.Pages
{
    public class EmailCreateModel(StarLIMSContext context) : PageModel
    {
        private readonly StarLIMSContext _context = context;
        [BindProperty]
        public OutgoingEmail OutgoingEmail { get; set; } = default!; 
        [BindProperty]
        public IFormFile? Attachment { get; set; }

        public void OnGet()
        {
            OutgoingEmail = new OutgoingEmail
            {
                Status = "Pending",
                InsertedAt = DateTime.Now
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            OutgoingEmail.InsertedAt = DateTime.Now;

            _context.OutgoingEmails.Add(OutgoingEmail);


            //I may not be able to do attachments....
            /*
            if(Attachment != null && Attachment.Length > 0)
            {
                OutgoingEmail.AttachmentCount = 1;
                using (var stream = Attachment.OpenReadStream())
                {
                    DBFileStorage newFile = new()
                    {
                        FileExtension = Attachment.FileName.Split(".").Last(),
                        FileImageId = OutgoingEmail.AttachmentList ?? ""
                        
                    };
                }
            }
            */
            await _context.SaveChangesAsync();

            return RedirectToPage("./Emails");
        }
    }
}
