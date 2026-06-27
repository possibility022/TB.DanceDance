using Domain.Entities;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Projects <see cref="InviteLink"/> entities to their API contracts. Mirrors the Sharing/Transfers
/// features' mappers.
/// </summary>
internal static class InviteLinkMapper
{
    public static string ResolveUrl(string appWebsiteOrigin, string linkId)
        => $"{appWebsiteOrigin}/invite/{linkId}";

    /// <summary>An unredeemed link past its expiry shows as "Expired"; otherwise the stored status name.</summary>
    public static string ResolveStatus(InviteLink link)
    {
        if (link.Status == InviteLinkStatus.Active && link.ExpireAt <= DateTimeOffset.UtcNow)
            return "Expired";
        return link.Status.ToString();
    }

    public static InviteLinkResponse MapToResponse(InviteLink link, string appWebsiteOrigin) => new()
    {
        Id = link.Id,
        Url = ResolveUrl(appWebsiteOrigin, link.Id),
        GroupId = link.GroupId,
        EventId = link.EventId,
        CreatedBy = link.CreatedBy,
        CreatedAt = link.CreatedAt,
        ExpireAt = link.ExpireAt,
        Status = ResolveStatus(link),
        RedeemedByUserId = link.RedeemedByUserId,
        RedeemedAt = link.RedeemedAt,
    };
}
