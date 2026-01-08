using CAHFS_Emailer.Data;
using CAHFS_Emailer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CAHFS_Emailer.Pages
{
    public class EmailEditModel(StarLIMSContext context) : PageModel
    {
        private readonly StarLIMSContext _context = context;
        [BindProperty]
        public OutgoingEmail OutgoingEmail { get; set; } = default!;
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outgoingEmail = await _context.OutgoingEmails.FirstOrDefaultAsync(m => m.OrigRec == id);

            if (outgoingEmail == null)
            {
                return NotFound();
            }

            OutgoingEmail = outgoingEmail;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(OutgoingEmail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OutgoingEmailExists(OutgoingEmail.OrigRec))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Emails");
        }

        private bool OutgoingEmailExists(int id)
        {
            return _context.OutgoingEmails.Any(e => e.OrigRec == id);
        }
    }
}
