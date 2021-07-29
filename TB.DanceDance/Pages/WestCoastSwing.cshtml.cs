using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TB.DanceDance.Data;

namespace TB.DanceDance.Pages
{
    [Authorize]
    public class WestCoastSwingModel : PageModel
    {
        public void OnGet()
        {
        }

        public WestCoastSwingModel(ApplicationDbContext context)
        {

        }
    }
}
