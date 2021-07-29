using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.Pages.Shared.Components.VideoList
{
    public class VideoListViewComponent : ViewComponent
    {

        private readonly ApplicationDbContext context;

        public VideoListViewComponent(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await GetItemsAsync(DanceType.WestCoastSwing);
            return View(items);
        }
        private Task<List<VideoInformation>> GetItemsAsync(DanceType danceType)
        {
            return context.VideosInformation
                .ToListAsync();
        }
    }
}
