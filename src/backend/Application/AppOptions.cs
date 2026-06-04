namespace Application;

// Copied from TB.DanceDance.API/AppOptions.cs so the Sharing endpoints (which build the public
// share URL) can resolve AppWebsiteOrigin from the Application project.
// TODO: bind this copied type in Program.cs (the original is still bound in the API project).
public record AppOptions
{
    public const string Position = "AppOptions";
    public string AppWebsiteOrigin { get; set; }
}
