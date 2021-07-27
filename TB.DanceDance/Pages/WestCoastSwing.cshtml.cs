using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TB.DanceDance.Pages
{
    [Authorize]
    public class WestCoastSwingModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
