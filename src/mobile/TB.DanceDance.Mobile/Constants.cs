namespace TB.DanceDance.Mobile;

public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    public static string DatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)}";
    
    public const string VideosDatabaseFileName = "VideosSQLite.db3";

    public static string VideosDatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, VideosDatabaseFileName)}";
}