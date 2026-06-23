namespace TB.DanceDance.API.Contracts.Features.Invites
{
    /// <summary>
    /// Public/anonymous preview of an invite link, shown on the landing page before sign-in.
    /// </summary>
    public class InviteLinkInfoResponse
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>"Group" or "Event".</summary>
        public string TargetType { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public bool IsRedeemable { get; set; }
    }

    /// <summary>Result of a redemption attempt, returned only on success (200 OK).</summary>
    public class RedeemInviteLinkResponse
    {
        /// <summary>True when the caller already had membership/access (no-op, FR-010).</summary>
        public bool AlreadyMember { get; set; }
    }
}
