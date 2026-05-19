namespace TB.DanceDance.API.Features.Events;

public static class EventRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/events";

    public const string AddEvent = $"{Base}";
    public const string Videos = $"{Base}/{{eventId:guid}}/videos";
}
