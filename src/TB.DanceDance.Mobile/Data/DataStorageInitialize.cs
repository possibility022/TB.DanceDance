using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        Debug.WriteLine("Initializing data storage started");
        dbContext.Database.EnsureCreated();
        dbContext.Database.Migrate();
        Debug.WriteLine("Initializing data storage complete");
    }
}