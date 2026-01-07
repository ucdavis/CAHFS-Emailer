using CAHFS_Emailer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CAHFS_Emailer.Pages
{
    public class EmailsModel(StarLIMSContext context) : PageModel
    {
        private readonly StarLIMSContext _context = context;

        public void OnGet()
        {
            var emails = _context.OutgoingEmails.ToList();
            ViewData["Emails"] = emails;
        }
    }
}
