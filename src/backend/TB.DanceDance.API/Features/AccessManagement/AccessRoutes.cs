namespace TB.DanceDance.API.Features.AccessManagement;

public static class AccessRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/videos/accesses";

    public const string GetAll = $"{Base}";
    public const string GetUserAccess = $"{Base}/my";
    public const string RequestAccess = $"{Base}/request";
    public const string ManageAccessRequests = $"{Base}/requests";
}
