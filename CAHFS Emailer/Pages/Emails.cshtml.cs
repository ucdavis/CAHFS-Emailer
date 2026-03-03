using System.ComponentModel;
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
        private readonly int pageSize = 25;

        [BindProperty(SupportsGet = true)]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        [BindProperty(SupportsGet = true)]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }
        [BindProperty(SupportsGet=true)]
        public int PageNumber { get; set; } = 1;

        public int PageCount = 0;

        public async Task OnGetAsync()
        {
            if (PageNumber < 1)
            {
                PageNumber = 1;
            }
            int skipAmount = (PageNumber - 1) * pageSize;

            var emails = _context.OutgoingEmails
                .Where(e => DateOnly.FromDateTime(e.InsertedAt) >= StartDate && DateOnly.FromDateTime(e.InsertedAt) <= EndDate)
                .Where(e => string.IsNullOrEmpty(Status) || e.Status == Status)
                .OrderByDescending(e => e.InsertedAt)
                .Skip(skipAmount)
                .Take(pageSize)
                .ToList();

            var totalRecords = _context.OutgoingEmails
                .Where(e => DateOnly.FromDateTime(e.InsertedAt) >= StartDate && DateOnly.FromDateTime(e.InsertedAt) <= EndDate)
                .Where(e => string.IsNullOrEmpty(Status) || e.Status == Status)
                .Count();
            PageCount = (int)Math.Ceiling((double)totalRecords / pageSize);

            List<EmailWithAttachments> emailsWithAttachments = [];
            foreach (var email in emails)
            {
                var emailWithAttachments = new EmailWithAttachments()
                {
                    Email = email,
                    Attachments = new List<OutgoingEmailAttachment>()
                };

                if (email.AttachmentId != null && email.AttachmentId > 0)
                {
                    var attachment = await _emailService.GetAttachment((int)email.AttachmentId);
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
