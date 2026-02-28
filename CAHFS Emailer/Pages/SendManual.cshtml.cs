using CAHFS_Emailer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CAHFS_Emailer.Pages
{
    public class SendManualModel(IEmailSender sender, EmailService emailService) : PageModel
    {
        [BindProperty]
        public string To { get; set; } = null!;

        [BindProperty]
        public string From { get; set; } = null!;

        [BindProperty]
        public string Subject { get; set; } = null!;

        [BindProperty]
        public string Body { get; set; } = null!;

        [BindProperty]
        public IFormFile? Attachment { get; set; }

        public string? Message { get; set; }

        private readonly EmailService _emailService = emailService;
        private readonly IEmailSender _emailSender = sender;

        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Message = "Please fill in all required fields.";
                return Page();
            }

            try
            {
                var message = _emailService.CreateMessage(From, To, Subject, Body, Attachment);
                await _emailSender.SendEmail(message);
                Message = "Email sent successfully!";
                ModelState.Clear();
            }
            catch (Exception ex)
            {
                Message = $"Error sending email: {ex.Message}";
            }

            return Page();
        }
    }
}
