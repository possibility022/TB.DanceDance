using System;

namespace TB.DanceDance.API.Contracts.Features.Invites
{
    /// <summary>
    /// An invite link as seen by an admin (create + list). Includes the share URL.
    /// </summary>
    public class InviteLinkResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public Guid? GroupId { get; set; }
        public Guid? EventId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
        /// <summary>Active / Redeemed / Revoked / Expired.</summary>
        public string Status { get; set; } = string.Empty;
        public string? RedeemedByUserId { get; set; }
        public DateTimeOffset? RedeemedAt { get; set; }
    }

    public class ListInviteLinksResponse
    {
        public InviteLinkResponse[] InviteLinks { get; set; } = Array.Empty<InviteLinkResponse>();
    }
}
