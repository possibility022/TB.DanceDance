namespace TB.DanceDance.API;

/// <summary>
/// Application-level error for configuration / request problems surfaced by the API host.
/// Replaces the old <c>Domain.Exceptions.AppException</c> that lived in the (now removed)
/// Application project.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception innerException) : base(message, innerException) { }
}
