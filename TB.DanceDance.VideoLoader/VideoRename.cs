using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TB.DanceDance.Data.Db;

namespace TB.DanceDance.VideoLoader
{
    class VideoRename
    {
        private readonly ApplicationDbContext context;

        public VideoRename(string databaseConnectionString)
        {

            if (string.IsNullOrEmpty(databaseConnectionString))
                throw new ArgumentNullException(nameof(databaseConnectionString));
            
            ApplicationDbContext.ConnectionString = databaseConnectionString;
            context = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>());
        }

        private readonly IReadOnlyDictionary<string, string> newNames = new Dictionary<string, string>()
        {
            {"SalsaFever_1.mp4", "SoloSteps.mp4"},
            {"SalsaFever_2.mp4", "SoloSteps_Modern.mp4"},
            {"VID_20200907_193939.mp4", "BasicSteps_Explained.mp4"},
            {"VID_20200914_193853.mp4", "BasicSteps_Leading_Explained.mp4"},
            {"VID_20200907_193852.mp4", "BasicSteps.mp4"},
            {"VID_20200909_193537.mp4", "LeftSidePass.mp4"},
            {"VID_20200923_194015.mp4", "LeftSidePass_WithLeading.mp4"},
            {"VID_20200909_193722.mp4", "LeftSidePass_MaleSteps.mp4"},
            {"VID_20200923_194039.mp4", "UnderArmPass.mp4"},
            {"VID_20201005_193558.mp4", "LeftSidePass_SugarTuck_2.mp4"},
            {"VID_20200930_193557.mp4", "SugarTuck.mp4"},
            {"VID_20201019_195001.mp4", "Whip.mp4"},
        };

        public async Task Rename()
        {
            var list = await context.VideosInformation.ToListAsync();

            foreach (var videoInformation in list)
            {
                if (newNames.ContainsKey(videoInformation.Name))
                {
                    Log.Information("Renaming {oldName} to {newName}", videoInformation.Name, newNames[videoInformation.Name]);
                    videoInformation.Name = newNames[videoInformation.Name];
                    context.Update(videoInformation);
                }
            }

            context.SaveChanges();
        }

    }
}
