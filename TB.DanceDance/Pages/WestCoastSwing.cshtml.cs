using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TB.DanceDance.Data;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.Pages
{
    [Authorize]
    public class WestCoastSwingModel : PageModel
    {
        private readonly ApplicationDbContext context;

        public void OnGet()
        {
            WestCoastSwingVideos = context.VideosInformation.ToList();
        }

        public WestCoastSwingModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IList<VideoInformation> WestCoastSwingVideos { get; set; }
    }
}
