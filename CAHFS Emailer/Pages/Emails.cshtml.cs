using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using CAHFS_Emailer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CAHFS_Emailer.Pages
{
    public class EmailsModel(StarLIMSContext context, EmailService emailService) : PageModel
    {
        private readonly StarLIMSContext _context = context;
        private readonly EmailService _emailService = emailService;

        [BindProperty(SupportsGet = true)]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        [BindProperty(SupportsGet = true)]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        public async Task OnGetAsync()
        {
            var emails = _context.OutgoingEmails
                .Where(e => DateOnly.FromDateTime(e.InsertedAt) >= StartDate && DateOnly.FromDateTime(e.InsertedAt) <= EndDate)
                .Where(e => string.IsNullOrEmpty(Status) || e.Status == Status)
                .OrderByDescending(e => e.InsertedAt)
                .ToList();

            List<EmailWithAttachments> emailsWithAttachments = [];
            foreach (var email in emails)
            {
                var emailWithAttachments = new EmailWithAttachments()
                {
                    Email = email,
                    Attachments = new List<DBFileStorage>()
                };

                if (email.AttachmentList != null)
                {
                    var attachment = await _emailService.GetAttachment(email.AttachmentList);
                    if (attachment != null)
                    {
                        emailWithAttachments.Attachments.Add(attachment);
                    }
                }

                emailsWithAttachments.Add(emailWithAttachments);
            }
            ViewData["Emails"] = emailsWithAttachments;
        }
    }
}
