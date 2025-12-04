using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Library.Data;

namespace TB.DanceDance.Mobile.Data;

public class DataStorageInitialize : IMauiInitializeService
{
    private readonly VideosDbContext dbContext;

    public DataStorageInitialize(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public void Initialize(IServiceProvider services)
    {
        Serilog.Log.Information("Initializing data storage started");
        dbContext.Database.EnsureCreated();
        dbContext.Database.Migrate();
        Serilog.Log.Information("Initializing data storage complete");
    }
}